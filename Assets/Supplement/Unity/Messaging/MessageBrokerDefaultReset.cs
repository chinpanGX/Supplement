using Supplement.Core;
using UnityEngine;

namespace Supplement.Unity
{
    internal static class MessageBrokerDefaultReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // ドメインリロードを無効にしていると、前回のプレイの(破棄済みのオブジェクトの)購読が残っているため消す。
            MessageBroker.Default.UnsubscribeAll();
        }
    }
}
