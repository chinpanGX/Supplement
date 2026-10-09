# Supplement ドキュメント

Unityのゲーム開発で使う補助的な機能をまとめたパッケージ(`com.chinpangx.supplement`)のドキュメントです。
このドキュメントはリポジトリのコードと一緒にバージョン管理しています。タグを切り替えると、そのバージョンで使える機能を確認できます。

## 利用者向け

Supplementを使ってゲームを作る人向けのページです。

- [導入](getting-started.md) — インストール方法・依存パッケージ・アセンブリの構成
- [変更履歴](changelog.md) — バージョンごとの変更。互換性が壊れる変更もここに書きます

### 機能一覧

| 機能 | 概要 | アセンブリ |
|---|---|---|
| [起動時の初期化](features/boot-initializer.md) | `IBootInitializationTask`を優先度順に実行する`BootInitializer` | Core |
| [セーブデータ](features/save-data.md) | 暗号化したファイルへの保存、Entity単位のリポジトリ、まとめて保存するバッファ | Core / Unity |
| [MessagePackのセーブデータ](features/messagepack-save-data.md) | セーブデータをMessagePackのバイナリで保存する。圧縮と改ざん検知つき | MessagePack |
| [メッセージング](features/messaging.md) | アプリ全体の`IMessageBroker`と、ヒエラルキーの中だけで届く`HierarchyMessageBroker` | Core / Unity / ZeroMessenger |
| [多重実行の防止](features/tap-guard.md) | 非同期処理の再入を防ぐ`TapGuard` | Core |
| [コレクション](features/collections.md) | 中身で比較できる`EquatableReadOnlyList`、UnityEngineに依存しない一時リスト`ScopedList`・`RentedList`、`DisposableBag` | Core |
| [列挙型のキャッシュ](features/enum-cache.md) | 列挙型の値と名前をキャッシュする`EnumCache` | Core |
| [CRC-32](features/crc32.md) | CRC-32を計算する`Crc32` | Core |
| [リストの描画](features/recycle-renderer.md) | DTOの配列をプールしたGameObjectに描画する`RecycleRenderer` | Unity |
| [Updateの集約](features/update-dispatcher.md) | MonoBehaviourのUpdateを1か所から呼ぶ`UpdateDispatcher` | Unity |
| [戻るボタン](features/back-key.md) | Androidの戻るボタン/Escで最前面のButtonを押す`BackKeyReceiver` | Unity |
| [アセットの読み込み](features/asset-loader.md) | `IAssetLoader`・`ISceneLoader`とAddressablesの実装 | Loader |
| [DIの補助](features/di-registration.md) | `IObjectResolver`への静的な入り口`ObjectResolverGateway`と、`ComponentExtensions` | Unity |

## メンテナー向け

Supplement自体を開発・修正する人向けのページです。

- [パフォーマンスとGC](maintainers/performance.md) — GCアロケーションを起こさないための実装の方針
- [内部の設計](maintainers/internals.md) — 利用者向けのページに書いていない実装の仕組みと、その理由
- [テスト](maintainers/testing.md) — テストの回し方と、GCアロケーション・入力を使うテストの書き方
