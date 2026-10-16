# テスト(メンテナー向け)

テストは`Assets/Supplement.Tests/`にあります。

| フォルダ | 種類 | 内容 |
|---|---|---|
| `_EditMode/` | EditMode | 各機能の動作と、GCアロケーションのテスト |
| `_MessagePack/` | EditMode | MessagePackのセーブデータと`AesHmacCryptoAlgorithm`のテスト(MessagePackを参照するため、アセンブリを分けている) |
| `_PlayMode/` | PlayMode | PlayerLoop・入力・画面・`OnDestroy`を使うテスト(`UpdateDispatcher`・`BackKeyReceiver`・`HierarchyMessageBroker`の破棄・サンプル画面) |
| `Domain/`・`Application/`・`Infrastructure/`・`Presentation/` | — | テストとサンプルで使う型(リポジトリ・画面など) |

## 回し方

[Unity CLI](https://unity.com/)で、開いているUnity Editorに対して回します。

```
unity command eval "UnityEditor.AssetDatabase.Refresh();"
unity command recompile
unity command run_tests --mode editor --timeout 600

unity command run_tests --mode playmode --filter TestUpdateDispatcher --async_tests true
unity command test_status    # "status"が"completed"になるまで繰り返す
unity command run_tests --mode playmode --filter TestBackKeyReceiver --async_tests true
unity command test_status
unity command run_tests --mode playmode --filter TestHierarchyMessageBrokerLifetime --async_tests true
unity command test_status
```

- Editorの外でファイルを変えたときは、Editorが気づくまでコンパイルされません。先に`AssetDatabase.Refresh`を呼びます(これでコンパイルまで済み、続く`recompile`が「up_to_date」を返すことがあります)。エラーの有無は`unity command console_status`の`compilationFailed`で確かめます
- PlayModeは`--async_tests true`で開始し、`test_status`で結果を取ります。待つ形(`--async_tests`なし)で呼ぶと、プレイモードに入らずに0件で返ります
- `--filter`は名前の部分一致です(正規表現ではない)。複数のクラスを回すときは、クラスごとに分けて呼びます
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
