using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Supplement.Unity.Editor")]
// MessagePack版のファイル読み書きが、読み書きの流れ(EncryptedFileIO)と一時ファイルからの置き換え・リトライ(SafeFile)を共有するため
[assembly: InternalsVisibleTo("Supplement.MessagePack")]
