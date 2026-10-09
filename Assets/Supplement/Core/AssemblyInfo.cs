using System.Runtime.CompilerServices;

// MessagePack版の暗号化が、ヘッダの読み書きと鍵の導出(AesFormat)を共有するため
[assembly: InternalsVisibleTo("Supplement.MessagePack")]
