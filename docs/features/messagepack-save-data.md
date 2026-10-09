# MessagePackのセーブデータ

アセンブリ: `Supplement.MessagePack`(別パッケージ `com.chinpangx.supplement.messagepack`)

[セーブデータ](save-data.md)の`IFileStorageService`を、JSONではなくMessagePackのバイナリで読み書きする実装に差し替えます。`SaveDataRepository`・`DeferredUpdateBuffer`などはそのまま使えます。

JSON版と比べて、次の点が違います。

- ファイルが小さく、変換が速い(数値キーならキー名を保存せず、LZ4で圧縮する)
- `Dictionary`やプロパティなど、`JsonUtility`で扱えない型も保存できる
- 改ざんや破損を検知する(HMAC-SHA256)。書き換えた・壊れたファイルは読み込みで例外になる

## 主な型

| 型 | 役割 |
|---|---|
| `MessagePackFileAccessor` | MessagePackでバイナリにし、`ICryptoAlgorithm`で暗号化してファイルに読み書きする(`IEncryptedFileAccessor`の実装) |
| `AesHmacCryptoAlgorithm` | AESで暗号化し、HMAC-SHA256で改ざんを検知する(`RegisterMessagePackFileStorage()`が`ICryptoAlgorithm`として登録する) |
| `MessagePackFileAccessor.DefaultSerializerOptions` | 既定のシリアライズ設定(`StandardResolver` + LZ4) |

## DIへの登録

`IContainerBuilder`(VContainer)の拡張メソッド`RegisterMessagePackFileStorage()`で、`IFileStorageService`をMessagePack版で登録します。JSON版の`RegisterEncryptedFileStorage()`の代わりに呼びます(両方は呼ばない)。

```csharp
using Supplement.MessagePack;

builder.RegisterMessagePackFileStorage();

// シリアライズの設定や暗号化の設定を変える場合
builder.RegisterMessagePackFileStorage(
    serializerOptions: MessagePackSerializerOptions.Standard
        .WithResolver(myResolver)
        .WithCompression(MessagePackCompression.Lz4BlockArray),
    aesOptions: AesOptions.CreateDefault());
```

`serializerOptions`を渡すと、既定の設定(`DefaultSerializerOptions`)の代わりにそれだけを使います。圧縮したい場合は、渡す設定にも`WithCompression`を付けてください。

## 使い方

保存するDTOに`[MessagePackObject]`と`[Key]`を付けます。それ以外は[セーブデータ](save-data.md)と同じです。

```csharp
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute; // VContainerにも[Key]があるため

[MessagePackObject]
public sealed class MissionDto
{
    [Key(0)] public int MissionId { get; set; }
    [Key(1)] public int Progress { get; set; }
    [Key(2)] public Dictionary<string, int> Counters { get; set; }
}

public sealed class MissionRepository : SaveDataRepository<int, Mission, MissionDto>
{
    // FileKey・Password・変換はJSON版と同じように実装する
}
```

DTOに`Vector3`などUnityの型を入れる場合は、MessagePackのUnity拡張([導入](../getting-started.md))を入れ、`UnityResolver`を含む設定を渡します。

```csharp
builder.RegisterMessagePackFileStorage(
    serializerOptions: MessagePackSerializerOptions.Standard
        .WithResolver(MessagePack.Unity.UnityResolver.InstanceWithStandardResolver)
        .WithCompression(MessagePackCompression.Lz4BlockArray));
```

## 動き

- 保存先・ファイル名・書き込みの安全性(一時ファイルからの置き換え、ロック中のリトライ、同じファイルへの操作を順に実行)は[セーブデータ](save-data.md)と同じです
- 中身はMessagePackのバイナリを(既定の設定では)LZ4で圧縮し、AESで暗号化して、末尾に改ざん検知の値を付けたものです。保存のたびにランダムなソルトを作るため、同じ内容でも毎回違うバイト列になります
- 書き換えた・壊れたファイル(途中で切れたファイル、ヘッダの壊れたファイル、JSON版のファイルを含む)や、パスワードの違うファイルは、読み込みで`CryptographicException`になります。壊れたセーブを作り直す処理は、この例外を捕まえて行えます

## 注意

- **`[Key]`の番号は、一度使ったら変えない・使い回さない**でください。古いセーブが読めなくなったり、別の項目として読まれたりします。項目を足すときは、新しい番号を付けます
- JSON版で保存したファイルは読めません(形式が違うため例外になります)。JSON版から移るときは、アプリ側で古いファイルを読み直して保存し直してください
- IL2CPPのビルドでは、MessagePackのSource Generatorが作るシリアライザを使います。MessagePackのNuGetパッケージ(Source Generatorを含む)を入れてください。入れ方は[導入](../getting-started.md)
- 暗号化のパスワードはコードに書くため、改ざん検知があっても強い保護にはなりません。手軽な書き換えと破損を検知できる程度のものです
