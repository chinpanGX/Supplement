namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="UpdateDispatcher"/>から毎フレーム呼ばれる処理。
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// 毎フレーム、PlayerLoopのUpdateで呼ばれる。例外を投げてもログに出すだけで、ほかの登録の呼び出しは続く。
        /// </summary>
        void OnUpdate();
    }
}