namespace Supplement.Core
{
    /// <summary>
    /// デバッグ用の設定値を永続化するためのストア。
    /// </summary>
    /// <remarks>
    /// ゲームのセーブデータ永続化には使用しないこと。セーブデータには <see cref="ISaveDataRepository"/> を使う。
    /// </remarks>
    public interface IDebugSettingsStore
    {
        bool GetBool(string key, bool defaultValue = false);
        void SetBool(string key, bool value);
    }
}
