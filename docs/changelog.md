# 変更履歴

## 1.4.0(未リリース)

### 互換性が壊れる変更
- `IFileStorageService.DeleteFile` → `DeleteFileAsync`、`ISaveDataRepository.Delete` → `DeleteAsync`。削除のリトライの待ちでメインスレッドを止めないため
- `HierarchyMessageBroker.Subscribe`が`IDisposable`を返すようになった(破棄で購読を解除できる)
- `TapGuard.BeginGuard`の戻り値が構造体の`TapGuard.GuardHandle`になった
- `EquatableReadOnlyList<T>`・`ToEquatableReadOnlyList`・`RecycleRenderer`のDTOの型に`IEquatable<T>`が必要になった
- 名前空間をアセンブリ名にそろえた
  - `EquatableReadOnlyList`・`EquatableReadOnlyListExtensions`: `Supplement` → `Supplement.Core`
  - `IAsyncRenderable<T>`: `Supplement.Unity` → `Supplement.Core`
  - `FileStorageService`・`EncryptedFileAccessor`・`EncryptedBinFileFormatProvider`: `Supplement.Unity.IO` → `Supplement.Unity`
  - VContainerへの登録の拡張メソッド: グローバル名前空間の`_DIExtensions` → `Supplement.Unity`の`ContainerBuilderExtensions`(`using Supplement.Unity;`が必要になる)
- 使われていなかった空のインターフェース`IAsyncRenderable`(型引数なし)を削除した
- `BackKeyReceiver`のGame Viewへの判定点の表示(`OnGUI`)を削除し、`IDebugOverlayState`・`BackKeyDebugOverlayState`・`IDebugSettingsStore`・`PlayerPrefsDebugSettingsStore`を削除した。リリースビルドでもReceiverごとに`OnGUI`が毎フレーム走っていたため。判定点はScene Viewで確かめる(下の「追加」)。Receiverの一覧だけを登録する`RegisterBackKeyDebugRegistry()`を追加し、`RegisterBackKeyDebugOverlay()`は`[Obsolete]`にした(同じ登録をする)
- `ISceneLoader.LoadSceneAsync`・`ChangeScene`は、読み込みを開始した後は取り消しを受け付けなくなった(`token`は開始前にだけ確かめる)。Unityはシーンの読み込みを途中で止められず、取り消してもSingleなら今のシーンが入れ替わり、`activateOnLoad: false`ならアクティブ化待ちのまま後のシーン読み込みをすべて止めていたため。要らないシーンは返ったハンドルの`Dispose`でアンロードする
- `FileStorageService`のコンストラクタが、`EncryptedFileAccessor`ではなく`IEncryptedFileAccessor`を受け取るようになった。MessagePack版と共通にするため。`RegisterEncryptedFileStorage()`を使っていれば変更は要らない。自分で登録している場合は`builder.Register<IEncryptedFileAccessor, EncryptedFileAccessor>(Lifetime.Singleton)`にする

### 追加
- [MessagePackのセーブデータ](features/messagepack-save-data.md)(別パッケージ`com.chinpangx.supplement.messagepack` 1.0.0): セーブデータをMessagePackのバイナリで保存する。LZ4の圧縮と、HMAC-SHA256の改ざん検知つき
- `IEncryptedFileAccessor`: `FileStorageService`が使う、データの変換と暗号化の差し替え口
- [UpdateDispatcher](features/update-dispatcher.md): MonoBehaviourのUpdateを1か所にまとめる
- [ScopedList / RentedList](features/collections.md)・[EnumCache](features/enum-cache.md)
- `HierarchyMessageBroker.Publish(T)`(インスタンス版)
- [BackKeyReceiver](features/back-key.md)がInput Systemに対応(Active Input HandlingがInput Systemだけでも動く)
- [BackKeyReceiver](features/back-key.md)を選択すると、判定点と枠をScene Viewに水色で表示する

### 修正
- `ObjectResolverGateway.TryResolve`が、未登録の型で例外を投げていた
- `HierarchyMessageBroker`の購読を解除する手段が無かった
- 同じファイルへの削除と書き込みが重なると、書き込んだファイルが消えることがあった(同じファイルへの操作を順に実行するようにした)
- `SaveDataRepository.DeleteAsync`の後の`SaveAsync`で、削除したはずのデータが書き戻されていた
- `SaveDataRepository.LoadAsync`の読み込み中に`DeleteAsync`を呼ぶと、読み込んだデータがメモリに戻り、次の保存で書き戻されていた
- `SaveDataRepository`で、`SaveAsync`・`DeleteAsync`の完了を待たずに`LoadAsync`を呼ぶと、保存前の状態で「データ無し」と判断したり、`FileNotFoundException`を投げたりしていた
- `Time.timeScale`が0の間にファイルの書き込み・削除がリトライになると、待ちが終わらなかった
- `AddressablesAssetLoader.LoadAssetAsync`の失敗時のメッセージが「シーン」になっていた
- `AddressablesAssetLoader`で、アセットの読み込みの取り消し・失敗のときや、シーンの読み込みの失敗のときにハンドルが解放されず、メモリに残っていた
- `DeferredUpdateBuffer.CommitAsync`の保存を待つ間に追加した更新が消えていた。また、保存に失敗した分が次のコミットで保存されていた
- `SafeFile`で、古い一時ファイルを消せない(`UnauthorizedAccessException`)と書き込みまで失敗していた
- `DisposableBag`の`Dispose`・`Clear`で、1つが例外を投げると残りが破棄されなかった
- `EncryptedFileAccessor`に`null`や空文字のパスワードを渡したときの例外が、原因の分からないものになっていた
- `RecycleRenderer`が、Factoryの破棄時に破棄済みの要素で例外を投げていた。また、描画に失敗した後は同じ内容で描画し直せなかった
- ドメインリロードを無効にしていると、`ObjectResolverGateway`に前のプレイの登録が残っていた

### 改善
- GCアロケーションを減らした: `EquatableReadOnlyList`の`foreach`・比較、`HierarchyMessageBroker`の発行、`Crc32`、`AesCryptoAlgorithm`の復号、`TapGuard`
- `BackKeyReceiver`の入力の監視を1か所にまとめた。画面を閉じたフレームに、下の画面のボタンが続けて発火しないようにした
- 公開している型・メソッド・プロパティに、XMLドキュメントコメント(引数・戻り値・投げる例外)を付けた

## 1.3.1
- [起動時の初期化](features/boot-initializer.md)(`BootInitializer`・`IBootInitializationTask`)を追加

## 1.3.0
- [RecycleRenderer](features/recycle-renderer.md)・`RecycleRendererFactory`を追加
- [BackKeyReceiver](features/back-key.md)・[TapGuard](features/tap-guard.md)を追加
- `Supplement.DI`を`Supplement.Unity`に統合。`ObjectResolverGateway`を`Supplement.Unity`へ移動

## 1.2.0
- セーブデータのAPIから`CancellationToken`を削除
- クラッシュに強いファイルの書き込み(一時ファイルからの置き換え)と、ロック中のファイルのリトライを追加

## 1.1.0
- `IAssetLoader.LoadAssetsByLabelAsync`、`ISceneLoader.LoadSceneAsync`の進捗の通知を追加
