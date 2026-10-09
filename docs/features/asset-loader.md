# アセットの読み込み

アセンブリ: `Supplement.Loader.Abstractions`(抽象) / `Supplement.Loader.AddressablesLoader`(実装)

アセットとシーンの読み込みを抽象化します。アプリのコードは`IAssetLoader`・`ISceneLoader`だけに依存し、実装(Addressables)はDIで差し込みます。

## 主な型

| 型 | 役割 |
|---|---|
| `IAssetLoader` | アドレス指定・ラベル指定でアセットを読み込む |
| `IAssetHandle<T>` | 読み込んだアセット。`Dispose`で解放する |
| `ISceneLoader` | シーンの読み込み(`LoadSceneAsync`)・切り替え(`ChangeScene`)、アクティブなシーンの設定(`SetActiveScene`)と名前の取得(`GetActiveSceneName`) |
| `ISceneHandle` | 読み込んだシーン。`ActivateAsync`で後からアクティブにできる。`Dispose`でアンロードする |
| `AddressablesAssetLoader` | `IAssetLoader`・`ISceneLoader`のAddressablesによる実装 |
| `AssetHandleExtensions.DisposeAll` | ハンドルのリストをまとめて`Dispose`する |
| `AssetLoadFailedException` | 読み込みに失敗したときの例外 |

## DIへの登録

登録用の拡張メソッドはありません。`AddressablesAssetLoader`を、使うインターフェースとして直接登録します。

```csharp
builder.Register<AddressablesAssetLoader>(Lifetime.Singleton).As<IAssetLoader, ISceneLoader>();
```

## 使い方

```csharp
// 1つ読み込む
using var handle = await assetLoader.LoadAssetAsync<Sprite>("Thumbnail/1001", token);
image.sprite = handle.Result;

// ラベルで複数読み込む
var handles = await assetLoader.LoadAssetsByLabelAsync<GameObject>("Enemy", token);
// ...
handles.DisposeAll();

// シーン(進捗を受け取る)
var scene = await sceneLoader.LoadSceneAsync("Battle", additive: true, activateOnLoad: true, token, progress);
```

## 動き

- アドレス・ラベルが`null`か空文字なら`ArgumentNullException`を投げます
- 読み込みに失敗すると`AssetLoadFailedException`を投げ、エラーログを出します。取り消し(`OperationCanceledException`)は警告ログを出して投げ直します
- アセットの読み込みは、失敗・取り消しのときに読み込み途中のハンドルを解放します。ラベル指定の読み込みで1つでも失敗・取り消しされると、読み込み済みのものもすべて解放します
- ラベルに当てはまるアセットが無ければ、空のリストを返します
- `ChangeScene`は、`activateOnLoad: true`で`LoadSceneAsync`を呼ぶのと同じです
- シーンの読み込み(`LoadSceneAsync`・`ChangeScene`)は、開始した後は取り消せません。Unityはシーンの読み込みを途中で止められないためです。`token`は開始前にだけ確かめ、取り消されていれば読み込まずに`OperationCanceledException`を投げます。開始した後は読み終えたハンドルを返すので、要らなければ`Dispose`でアンロードしてください
- ハンドルは二重に`Dispose`しても安全です。`Dispose`の後に`Result`を読むと`ObjectDisposedException`になります
- `activateOnLoad: false`で読み込んだシーンは、`ISceneHandle.ActivateAsync`でアクティブにします。`activateOnLoad: true`で読み込んだシーンや、一度アクティブにしたシーンで呼ぶと`InvalidOperationException`になります
