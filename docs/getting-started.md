# 導入

## インストール

Package Managerの「Add package from git URL...」から、次のURLを追加します。

```
https://github.com/chinpanGX/Supplement.git?path=Assets/Supplement
```

`IMessageBroker`をZeroMessengerで実装したパッケージは別になっています。使う場合は次のURLも追加します。

```
https://github.com/chinpanGX/Supplement.git?path=Assets/Supplement.ZeroMessenger
```

セーブデータをMessagePackで保存するパッケージも別になっています。使う場合は次のURLを追加し、MessagePack(下の「依存パッケージ」)も入れます。

```
https://github.com/chinpanGX/Supplement.git?path=Assets/Supplement.MessagePack
```

特定のバージョンを使う場合は、URLの末尾に`#1.4.0`のようにタグを付けます。

## 依存パッケージ

| パッケージ | 用途 |
|---|---|
| UniTask(2.5.11以上) | 非同期処理 |
| VContainer(1.19.0以上) | DIコンテナへの登録、`RecycleRenderer`での生成 |
| Addressables(2.7.6以上) | `AddressablesAssetLoader` |
| ZeroMessenger(`Supplement.ZeroMessenger`を使う場合) | `GlobalMessageBroker` |
| MessagePack 3.x(`Supplement.MessagePack`を使う場合) | MessagePackのセーブデータ。本体(`MessagePack.dll`とSource Generator)はNuGetにしか無いため、NuGetForUnityで`MessagePack`を入れる |
| MessagePackのUnity拡張(任意) | DTOに`Vector3`などUnityの型を入れる場合だけ要る。Package Managerで`https://github.com/MessagePack-CSharp/MessagePack-CSharp.git?path=src/MessagePack.UnityClient/Assets/Scripts/MessagePack#v3.1.9`のように、NuGetの本体と同じ版のタグを付けて入れる |
| Input System(任意) | 入っていれば`BackKeyReceiver`がInput Systemから戻るボタンを読む。無ければ旧Input Managerを使う |

## アセンブリの構成

| アセンブリ | 内容 | Unityへの依存 |
|---|---|---|
| `Supplement.Core` | 抽象(インターフェース)と、Unityに依存しない実装 | なし(`noEngineReferences`) |
| `Supplement.Unity` | Unityに依存する実装(ファイルIO、メッセージング、UI部品、DIの拡張メソッド) | あり |
| `Supplement.Loader.Abstractions` | アセットの読み込みの抽象 | あり |
| `Supplement.Loader.AddressablesLoader` | Addressablesによる実装 | あり |
| `Supplement.Unity.Editor` | エディタ拡張(BackKey Event Viewer) | エディタのみ |
| `Supplement.ZeroMessenger`(別パッケージ) | `IMessageBroker`のZeroMessengerによる実装 | あり |
| `Supplement.MessagePack`(別パッケージ) | セーブデータのMessagePackによる実装 | あり |

名前空間はアセンブリ名と同じです。VContainerへの登録に使う拡張メソッド(`RegisterEncryptedFileStorage`など)は`Supplement.Unity`にあるので、`using Supplement.Unity;`が必要です。

## 対応するUnity・CSharp

- Unity 6(C# 9)
- IL2CPPのビルドを前提にしています
