# CRC-32(Crc32)

アセンブリ・名前空間: `Supplement.Core`

CRC-32(IEEE 802.3、多項式`0xEDB88320`)を計算します。一般的なCRC-32と同じ値になります(`"123456789"`は`0xCBF43926`)。

## 主なメンバー

| メソッド | 戻り値 |
|---|---|
| `Compute(ReadOnlySpan<byte>)` | CRC-32を`uint`で返す |
| `Compute(string)` | 文字列をUTF-8にしたもののCRC-32を、ゼロ埋めした8桁の16進数(小文字)の文字列で返す |

## 使い方

```csharp
uint checksum = Crc32.Compute(bytes);        // byte[]やSpanをそのまま渡せる
string key = Crc32.Compute("deviceCredentials");  // "98e11467"
```

## 動き

- `Compute(ReadOnlySpan<byte>)`はGCアロケーションを起こしません
- `Compute(string)`は、UTF-8への変換に`ArrayPool`の配列を使います。確保するのは戻り値の文字列だけです

## 注意

- `Compute(string)`は[セーブデータ](save-data.md)のファイル名に使っています。そのため出力は今後も変わりません
- 改ざんの検出には使えません(暗号学的なハッシュではありません)。データの破損の検出や、キーを短い名前にする用途に使います
