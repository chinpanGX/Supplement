using System.Collections.Generic;

namespace Supplement.Unity
{
    /// <summary>
    /// 現在有効な<see cref="BackKeyReceiver"/>を、Registerされた順に保持する。
    /// DIコンテナにSingletonとして登録して使う。
    /// </summary>
    internal sealed class BackKeyReceiverDebugRegistry
    {
        private readonly List<BackKeyReceiver> receivers = new();

        public IReadOnlyList<BackKeyReceiver> Receivers => receivers;

        internal void Register(BackKeyReceiver receiver)
        {
            receivers.Add(receiver);
        }

        internal void Unregister(BackKeyReceiver receiver)
        {
            receivers.Remove(receiver);
        }
    }
}
