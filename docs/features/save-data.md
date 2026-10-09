# セーブデータ

アセンブリ: `Supplement.Core`(抽象・リポジトリ・暗号化) / `Supplement.Unity`(ファイルIO)

端末にデータを暗号化して保存します。ファイルの読み書きだけを使う方法と、Entity単位で扱うリポジトリの2段階があります。

このページはJSON版の説明です。MessagePackのバイナリで保存する場合は[MessagePackのセーブデータ](messagepack-save-data.md)を使います。

## 主な型

| 型 | 役割 |
|---|---|
| `IFileStorageService` / `FileStorageService` | キーを指定して、暗号化したファイルを読み書き・削除する |
| `SaveDataRepository<TKey, TEntity, TDto>` | Entityを辞書で持ち、まとめてファイルに保存・読み込みするリポジトリの基底クラス |
| `ISaveDataRepository` | リポジトリの読み込み・保存・削除。複数のリポジトリをまとめて扱うときに使う |
| `DeferredUpdateBuffer<TEntity>` | 更新を溜めておき、`CommitAsync`でまとめて保存する(`Rollback`で破棄) |
| `AesOptions` | 暗号化の設定。`AesOptions.CreateDefault()`はAES-128・CBC・PKCS7・PBKDF2(SHA-256、1000回) |

## 保存のされ方

- 保存先は`Application.persistentDataPath/SaveData/`です。`SetDirectoryName`でディレクトリ名を変えられます(同じPCで複数のインスタンスを動かす場合など)。`null`か空文字を渡すと`SaveData`に戻ります
- ファイル名は、キーの[CRC-32](crc32.md)を16進数にしたもの(`{crc32}.bin`)です
- 中身は`JsonUtility`でJSONにしたものをAESで暗号化しています。保存のたびにランダムなソルトを作るため、同じ内容でも毎回違うバイト列になります
- 書き込み中にクラッシュしても、元のファイルは壊れません
- セキュリティソフトなどがファイルを一時的に開いていても、少し待ってリトライします
- 同じファイルへの読み込み・書き込み・削除は、呼んだ順に1つずつ実行します。前の操作の完了を待たずに続けて呼んでも構いません
- `Exists`(リポジトリの`IsCreated`)は順番を待たず、今の状態を返します。待っていない書き込み・削除がある場合は、`ReadAsync`がファイルの無いときに投げる`FileNotFoundException`で判断します
- リポジトリの`DeleteAsync`は、ファイルと一緒に手元のEntityも空にします

## DIへの登録

`IContainerBuilder`(VContainer)の拡張メソッドで、ファイルの読み書きに必要な型(`IFileStorageService`・暗号化の実装・`AesOptions`)をまとめて登録します。

| メソッド | 暗号化の設定 |
|---|---|
| `RegisterEncryptedFileStorage()` | `AesOptions.CreateDefault()` |
| `RegisterEncryptedFileStorageWithCustomAesOptions(AesOptions)` | 指定した`AesOptions` |

```csharp
builder.RegisterEncryptedFileStorage();

// 保存先のディレクトリ名を変える場合
builder.RegisterBuildCallback(resolver =>
    resolver.Resolve<IFileStorageService>().SetDirectoryName("SaveData_Slot1"));
```

リポジトリ(`SaveDataRepository`の派生)は、アプリ側で`builder.Register`して登録します。

## 使い方: ファイルを直接読み書きする

```csharp
// 1件だけ保存したい場合
public sealed class DeviceCredentialsRepository
{
    private const string FileKey = "deviceCredentials";
    private const string Password = "...";
    private readonly IFileStorageService fileStorageService;

    public async UniTask<CredentialsDto> LoadAsync()
    {
        try
        {
            return await fileStorageService.ReadAsync<CredentialsDto>(FileKey, Password);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public UniTask SaveAsync(CredentialsDto dto)
    {
        fileStorageService.CreateDirectoryIfNotExists(FileKey);
        return fileStorageService.WriteAsync(FileKey, dto, Password);
    }
}
```

保存する型は`JsonUtility`で変換できる型(`[Serializable]`のクラスと公開フィールド)にします。

## 使い方: リポジトリにする

`SaveDataRepository`を継承し、`FileKey`・`Password`・DTOとEntityの変換・辞書への反映(`UpdateCore`)を実装します。

- `LoadAsync`でファイルから読み込みます。ファイルが無ければ何もしません。変換に失敗したDTOが1件でもあると、`SaveDataRepositoryException`を投げ、辞書の中身は変えません。読み込みの途中で`DeleteAsync`を呼ぶと、読み込んだ内容は辞書に反映しません
- `UpdateAsync(List<TEntity>)`で辞書に反映してから、全件を保存します
- `DeferredUpdateBuffer`を使うと、複数のリポジトリの更新を溜めて、最後にまとめて保存できます。`CommitAsync`は呼んだ時点のバッファを切り離して保存するため、保存を待つ間に次の`Begin`・`Add`を始められます。保存に失敗した分はバッファに残りません
- `DeferredUpdateBuffer`は、`Begin`の後に`Add`し、`CommitAsync`か`Rollback`で終えます。`Begin`中にもう一度`Begin`したり、`Begin`の前に`Add`・`CommitAsync`・`Rollback`を呼んだりすると、`DeferredUpdateBufferException`になります

## 注意

- 暗号化のパスワードはコードに書くため、強い保護にはなりません。改ざんや覗き見を手軽にできなくする程度のものです
- パスワードに`null`や空文字は使えません(`ArgumentException`になります)
