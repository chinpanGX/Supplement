# テスト(メンテナー向け)

テストは`Assets/Supplement.Tests/`にあります。

| フォルダ | 種類 | 内容 |
|---|---|---|
| `_EditMode/` | EditMode | 各機能の動作と、GCアロケーションのテスト |
| `_MessagePack/` | EditMode | MessagePackのセーブデータと`AesHmacCryptoAlgorithm`のテスト(MessagePackを参照するため、アセンブリを分けている) |
| `_PlayMode/` | PlayMode | PlayerLoop・入力・画面を使うテスト(`UpdateDispatcher`・`BackKeyReceiver`・サンプル画面) |
| `Domain/`・`Application/`・`Infrastructure/`・`Presentation/` | — | テストとサンプルで使う型(リポジトリ・画面など) |

## 回し方

```
uloop compile
uloop run-tests --test-mode EditMode
uloop run-tests --test-mode PlayMode --filter-type regex --filter-value "TestUpdateDispatcher|TestBackKeyReceiver"
```

- PlayModeの`TestItemListView`は、表示した画面の閉じるボタンを手で押すまで終わりません(押さないと180秒でタイムアウトする)。自動で回すときは、上のようにクラスで絞ります
- このプロジェクトはプレイモードに入るときのドメインリロードを無効にしています。Unity Test Framework 1.8.0は、この設定だと2回目以降のPlayModeテストが0件になります(テストアセンブリの一覧のキャッシュが、`null`ではなく空のリストに戻されるため)。`_PlayMode/PlayModeTestAssemblyCacheWorkaround.cs`が、プレイモードに入る前にこのキャッシュを`null`に戻して回避しています。Test Framework側が直ったら消します

## GCアロケーションのテスト

「GCアロケーションを起こさない」とうたう処理には、Unity Test Frameworkの`Is.Not.AllocatingGCMemory()`でテストを付けます。

```csharp
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

[Test]
public void PublishDoesNotAllocate()
{
    broker.Publish(new SampleMessage());   // 初回の生成(キャッシュ・プールの配列)を計測から外す

    Assert.That(() => { broker.Publish(new SampleMessage()); }, Is.Not.AllocatingGCMemory());
}
```

- 計測の対象は**値を返さない**ラムダにします。値を返すラムダはNUnitが先に評価してしまい、計測の対象になりません
- 初回だけ起きる確保(静的キャッシュ、`ArrayPool`の配列)は、計測の前に1回呼んで外します
- 計測が効いていることを確かめるため、確保が起きるケース(インターフェース越しの`foreach`)を`Is.AllocatingGCMemory()`で確かめるテストも置いています(`TestEquatableReadOnlyList`)

## 出力を変えてはいけないもの

- `Crc32.Compute(string)`はセーブデータのファイル名に使っています。`TestCrc32`で、従来の出力と一致し続けることを確かめています。期待値を書き換えないでください

## 入力を使うテスト

`TestBackKeyReceiver`は、`InputSystem.AddDevice<Keyboard>()`で仮想キーボードを作り、`QueueStateEvent`でEscを押します。

- テスト中のUnityにはフォーカスが無いため、既定の設定ではキーボードの入力が捨てられます。テストの間だけ、`ScriptableObject.CreateInstance<InputSettings>()`で作った設定(`backgroundBehavior = IgnoreFocus`など)を`InputSystem.settings`に入れ、TearDownで元に戻します。プロジェクトの設定アセットは書き換えません
- 「発火しない」ことを確かめるテストが、入力が届かないまま通らないよう、押下が届いたことも確かめます
