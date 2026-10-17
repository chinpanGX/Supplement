using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity
{
    /// <summary>
    /// 自身より下の階層のコンポーネントが発行したメッセージを、型ごとに購読者へ届ける。
    /// </summary>
    /// <remarks>
    /// 発行・購読の動き(呼ぶ順番・例外の扱い・非同期の購読)は<see cref="MessageBroker"/>と同じ。
    /// GameObjectが破棄されると、購読をすべて解除する。
    /// <see cref="Publish{T}(Component,T)"/>は呼ぶたびに親を辿ってブローカーを探す。頻繁に発行する場合は、
    /// 探したブローカーを持っておいて<see cref="Publish{T}(T)"/>を呼ぶ。
    /// </remarks>
    public class HierarchyMessageBroker : MonoBehaviour
    {
        private readonly MessageBroker broker = new MessageBroker();

        /// <summary>
        /// <paramref name="component"/>から親を辿って最も近い<see cref="HierarchyMessageBroker"/>(非アクティブのものを含む)を探し、
        /// <paramref name="message"/>を発行する。見つからなければ何もしない。
        /// </summary>
        /// <param name="component">発行元のコンポーネント。</param>
        /// <param name="message">発行するメッセージ。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="component"/>がnull。</exception>
        public static void Publish<T>(Component component, T message) where T : struct
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            var broker = component.GetComponentInParent<HierarchyMessageBroker>(true);
            if (broker == null) return;

            broker.Publish(message);
        }

        /// <summary>
        /// <typeparamref name="T"/>を購読しているハンドラーに<paramref name="message"/>を送る。
        /// </summary>
        /// <remarks>
        /// 同期のハンドラーは、この呼び出しの中で登録順に呼ぶ。非同期のハンドラー(<see cref="SubscribeAwait{T}"/>)は
        /// 開始するだけで完了を待たない。
        /// ハンドラーが例外を投げても残りのハンドラーは呼び、例外は呼び出し元に投げずに
        /// <c>UniTaskScheduler.UnobservedTaskException</c>に流す。
        /// </remarks>
        /// <param name="message">送るメッセージ。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        public void Publish<T>(T message) where T : struct
        {
            broker.Publish(message);
        }

        /// <summary>
        /// <typeparamref name="T"/>のメッセージを購読する。
        /// </summary>
        /// <remarks>
        /// 同じデリゲートを重ねて登録すると、登録した回数だけ呼ぶ。
        /// </remarks>
        /// <param name="handler">メッセージを受け取ったときに呼ぶ処理。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>破棄すると購読をやめる。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            return broker.Subscribe(handler);
        }

        /// <summary>
        /// <typeparamref name="T"/>のメッセージを、非同期の処理で購読する。
        /// </summary>
        /// <remarks>
        /// <see cref="Publish{T}(T)"/>で開始したハンドラーが投げた例外は、<c>UniTaskScheduler.UnobservedTaskException</c>に流す。
        /// 購読の解除・<see cref="AsyncSubscribeStrategy.Switch"/>での切り替えで取り消された場合の
        /// <see cref="OperationCanceledException"/>は、例外として扱わない。
        /// </remarks>
        /// <param name="handler">
        /// メッセージを受け取ったときに呼ぶ処理。渡すトークンは、購読を解除したとき(<see cref="AsyncSubscribeStrategy.Switch"/>では
        /// 次のメッセージが届いたときも)とGameObjectが破棄されたときに取り消される。
        /// </param>
        /// <param name="subscribeStrategy">処理中に次のメッセージが届いたときの扱い。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>破棄すると購読をやめ、処理中のハンドラーに渡したトークンを取り消す。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/>がnull。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="subscribeStrategy"/>が定義されていない値。</exception>
        public IDisposable SubscribeAwait<T>(Func<T, CancellationToken, UniTask> handler,
            AsyncSubscribeStrategy subscribeStrategy = AsyncSubscribeStrategy.Sequential) where T : struct
        {
            return broker.SubscribeAwait(handler, subscribeStrategy);
        }

        /// <summary>
        /// 次に発行された<typeparamref name="T"/>のメッセージを1件だけ待つ。受け取ったら購読を解除する。
        /// </summary>
        /// <remarks>
        /// 呼んだ時点から購読するため、それより前に発行されたメッセージは受け取らない。
        /// GameObjectが破棄されても完了しないので、<c>destroyCancellationToken</c>などで取り消す。
        /// </remarks>
        /// <param name="cancellationToken">取り消すと購読を解除し、<see cref="OperationCanceledException"/>で完了する。</param>
        /// <typeparam name="T">メッセージの型。</typeparam>
        /// <returns>受け取ったメッセージで完了する。</returns>
        public UniTask<T> FirstAsync<T>(CancellationToken cancellationToken = default) where T : struct
        {
            return broker.FirstAsync<T>(cancellationToken);
        }

        // 破棄の後に子が発行しても例外にならないよう、ブローカーは破棄せず、購読の解除だけにする。
        private void OnDestroy()
        {
            broker.UnsubscribeAll();
        }
    }
}
