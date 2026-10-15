using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Core
{
    // MessageBrokerが型を知らずにチャンネルを片付けるためのインターフェース。
    internal interface IMessageChannel
    {
        // 購読をすべて解除する。チャンネルは引き続き使える。
        void Clear();

        // 購読をすべて解除し、以後の購読を受け付けない。
        void Close();
    }

    // 1つのメッセージの型の購読者を持ち、発行を届ける。
    // 購読者は、購読者自身をノードにした双方向リストで持つ。登録・解除はlockの中で前後をつなぎ替えるだけで、
    // 購読者の数によらず定数時間で、購読者1つのほかに確保もしない。
    // 発行はlockを取らずにリストを辿る。開始時の版より新しいノード(発行中に登録されたもの)に着いたら止め、
    // 解除済みのノードは飛ばす。解除したノードも次のノードへの参照を残すので、辿っている途中で解除されても先へ進める。
    internal sealed class MessageChannel<T> : IMessageChannel where T : struct
    {
        private readonly object gate = new object();
        private MessageSubscriber<T> root;
        // 最後に登録した購読者の版。64bitの読み書きを不可分にするため、Volatileで読み書きする。
        private long version;
        private bool closed;

        public IDisposable Add(MessageSubscriber<T> subscriber)
        {
            lock (gate)
            {
                if (closed) throw new ObjectDisposedException(nameof(MessageBroker));

                var newVersion = version + 1;
                subscriber.Version = newVersion;
                subscriber.IsLinked = true;

                // 版を進めてからつなぐ。発行が新しいノードを見つけたときには、必ずその版より古い版を控えている。
                Volatile.Write(ref version, newVersion);
                if (root == null)
                {
                    subscriber.Previous = subscriber;
                    Volatile.Write(ref root, subscriber);
                }
                else
                {
                    var last = root.Previous;
                    subscriber.Previous = last;
                    root.Previous = subscriber;
                    Volatile.Write(ref last.Next, subscriber);
                }
            }
            return subscriber;
        }

        // 購読者を解除済みにし、リストにつながっていれば外す。すでに解除済みならfalseを返す。
        public bool Remove(MessageSubscriber<T> subscriber)
        {
            lock (gate)
            {
                if (!subscriber.MarkDisposed())
                {
                    return false;
                }
                // Clearで外したノードは、印を立てるだけでよい。
                if (!subscriber.IsLinked)
                {
                    return true;
                }
                subscriber.IsLinked = false;

                var next = subscriber.Next;
                if (subscriber == root)
                {
                    if (next != null)
                    {
                        next.Previous = subscriber.Previous;
                    }
                    Volatile.Write(ref root, next);
                }
                else
                {
                    var previous = subscriber.Previous;
                    Volatile.Write(ref previous.Next, next);
                    if (next != null)
                    {
                        next.Previous = previous;
                    }
                    else
                    {
                        // 末尾を外したので、先頭が指す末尾を1つ前に戻す。
                        root.Previous = previous;
                    }
                }

                // Nextは辿っている途中の発行のために残す。Previousは発行で使わないので、前のノードを生かし続けないよう外す。
                subscriber.Previous = null;
                return true;
            }
        }

        public void Clear()
        {
            MessageSubscriber<T> removed;
            lock (gate)
            {
                removed = root;
                Volatile.Write(ref root, null);
                for (var node = removed; node != null; node = node.Next)
                {
                    node.IsLinked = false;
                    node.Previous = null;
                }
            }

            // リストから外したあとで破棄するので、購読者からのRemoveは解除済みの印を立てるだけになる。
            while (removed != null)
            {
                var next = removed.Next;
                removed.Dispose();
                removed = next;
            }
        }

        public void Close()
        {
            lock (gate)
            {
                closed = true;
            }
            Clear();
        }

        // ハンドラーが例外を投げても残りの購読者は呼ぶ。例外は呼び出し元に投げず、非同期のハンドラーと同じく
        // UniTaskScheduler.UnobservedTaskExceptionに流す。購読者の不具合で、発行した側の処理を止めないため。
        // 購読者を辿るループはtry/catchの無いInvokeFromに分け、例外が出たら残りの購読者からInvokeFromを呼び直す。
        // 同じメソッドの中でtry/catchとループを持つと、catchでも使うノードの参照をJITがレジスタに置けず、
        // 次のノードへ進むたびにスタックへの書き込みと読み出しが挟まって、購読者ごとのコストが増えるため。
        public void Publish(T message)
        {
            // 版を先に読む。後から読むと、発行中に登録された購読者に届いてしまうことがある。
            var startVersion = Volatile.Read(ref version);
            var node = Volatile.Read(ref root);
            // 最後に呼んだ購読者の版。例外が出たときに、どこから続けるかを決めるために使う。
            long invokedVersion = 0;
            while (node != null)
            {
                try
                {
                    InvokeFrom(node, startVersion, message, ref invokedVersion);
                    node = null;
                }
                catch (Exception e)
                {
                    UniTask.FromException(e).Forget();

                    // リストは登録順(版の順)に並んでいるので、例外を投げた購読者より新しい最初の購読者から続ける。
                    // 先頭から辿り直すのは例外が出たときだけ。
                    node = Volatile.Read(ref root);
                    while (node != null && node.Version <= invokedVersion)
                    {
                        node = Volatile.Read(ref node.Next);
                    }
                }
            }
        }

        // nodeから順に購読者を呼ぶ。例外はそのまま投げる。
        // 呼ぶ前に購読者の版をinvokedVersionに書くので、例外が出たときはそれが例外を投げた購読者の版になる。
        // 控えるのをノードの参照ではなく版(long)にするのは、参照を書くとノードごとにGCの書き込みバリアが入るため。
        // Publishに埋め込まれると、ループがtry/catchの中に入ってしまうので、埋め込ませない。
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InvokeFrom(MessageSubscriber<T> node, long startVersion, T message, ref long invokedVersion)
        {
            for (; node != null; node = Volatile.Read(ref node.Next))
            {
                // 登録順に並んでいるので、これより後ろもすべて発行中に登録されたもの。
                var nodeVersion = node.Version;
                if (nodeVersion > startVersion) break;
                if (node.IsDisposed) continue;

                invokedVersion = nodeVersion;

                // 同期の購読者は、仮想呼び出しを通さずにデリゲートを直接呼ぶ。
                var handler = node.SyncHandler;
                if (handler != null)
                {
                    handler(message);
                }
                else
                {
                    node.Invoke(message);
                }
            }
        }
    }
}
