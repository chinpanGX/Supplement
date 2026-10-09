using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Supplement.Loader.Abstractions
{
    /// <summary>
    /// シーンを非同期で読み込み、アクティブなシーンを切り替える。
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// <paramref name="address"/>のシーンを読み込む。
        /// </summary>
        /// <param name="address">シーンのアドレス。</param>
        /// <param name="additive">今のシーンに追加するならtrue。falseなら今のシーンと入れ替える。</param>
        /// <param name="activateOnLoad">
        /// 読み込み後すぐにアクティブ化するならtrue。falseにしたときは<see cref="ISceneHandle.ActivateAsync"/>でアクティブ化する。
        /// </param>
        /// <param name="token">
        /// 読み込みを始める前の取り消しにだけ使う。シーンの読み込みは途中で止められないため、始めた後は取り消しても読み込みを続ける。
        /// </param>
        /// <param name="progress">読み込みの進み具合(0から1)を受け取る。</param>
        /// <returns>読み込んだシーンのハンドル。破棄するとシーンをアンロードする。</returns>
        /// <exception cref="AssetLoadFailedException">読み込みに失敗した。</exception>
        UniTask<ISceneHandle> LoadSceneAsync(string address, bool additive, bool activateOnLoad,
            CancellationToken token, IProgress<float> progress = null);

        /// <summary>
        /// <paramref name="address"/>のシーンを読み込み、すぐにアクティブ化する。
        /// <see cref="LoadSceneAsync"/>を<c>activateOnLoad: true</c>で呼ぶのと同じ。
        /// </summary>
        /// <param name="address">シーンのアドレス。</param>
        /// <param name="additive">今のシーンに追加するならtrue。falseなら今のシーンと入れ替える。</param>
        /// <param name="token">読み込みを始める前の取り消しにだけ使う。</param>
        /// <returns>読み込んだシーンのハンドル。</returns>
        UniTask<ISceneHandle> ChangeScene(string address, bool additive, CancellationToken token);

        /// <summary>
        /// <paramref name="sceneHandle"/>のシーンをアクティブなシーンにする。
        /// </summary>
        /// <param name="sceneHandle">読み込んだシーンのハンドル。</param>
        void SetActiveScene(ISceneHandle sceneHandle);

        /// <summary>
        /// アクティブなシーンの名前を返す。
        /// </summary>
        string GetActiveSceneName();
    }
}