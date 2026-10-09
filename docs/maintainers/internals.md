# 内部の設計(メンテナー向け)

利用者向けのページには書いていない、実装の仕組みとその理由をまとめます。

## セーブデータ(SafeFile・FileStorageService)

- 書き込みは、`{ファイル名}.tmp{GUID}~`の一時ファイルに書いてから`File.Replace`(無ければ`File.Move`)で置き換えます。書き込みの前に、前回のクラッシュで残った一時ファイルを消します(消せなければ無視して書き込みを続けます)
- 書き込み・置き換え・削除は、セキュリティソフトなどによる一時的なロックに備えて、100msから始めて倍々に(上限5秒)最大10回まで試します。リトライする例外は、一時ファイルの書き込みでは`UnauthorizedAccessException`だけ(ディスク容量不足などの`IOException`はリトライしても直らないため)、置き換えと削除では`IOException`です。待ちは実時間で数えます(`Time.timeScale`が0の一時停止中でも終わるように)
- リトライの待ちがあるため、待っている間に別の操作が割り込めます。`FileStorageService`はファイルのパスごとに`SemaphoreSlim`を持ち、同じファイルへの読み込み・書き込み・削除を呼んだ順に1つずつ実行します(削除のリトライ中に書いたファイルを、再開した削除が消さないため)
- 暗号化の形式は`[Magic "SAC1"(4) | Version(1) | SaltLength(2) | Salt | Cipher]`です。鍵とIVはパスワードとソルトからPBKDF2で作ります
- `FileStorageService`は、データの変換と暗号化を`IEncryptedFileAccessor`に任せます。JSON版は`EncryptedFileAccessor`、MessagePack版は`MessagePackFileAccessor`です。パス・排他・リトライは共通なので、MessagePack版も`FileStorageService`をそのまま使います
- 引数の検証・読み込み・安全な書き込み・エラーログは、JSON版とMessagePack版で`EncryptedFileIO`(内部クラス)を共有します。違うのは変換と暗号化だけで、これはstaticラムダと`state`で渡します(呼び出しのたびにクロージャを作らないため)。MessagePack版は`Supplement.Unity`の`InternalsVisibleTo`で`EncryptedFileIO`と`SafeFile`を使います
- 暗号化のヘッダの読み書き・鍵の導出・ソルトの生成は、`AesCryptoAlgorithm`と`AesHmacCryptoAlgorithm`で`AesFormat`(`Supplement.Core`の内部クラス)を共有します。不正なデータで投げる例外の型は両者で違う(JSON版は従来どおり`InvalidOperationException`、MessagePack版は`CryptographicException`)ため、例外は呼び出し側から渡したデリゲートで作ります
- MessagePack版の暗号化(`AesHmacCryptoAlgorithm`)の形式は`[Magic "SAH1"(4) | Version(1) | SaltLength(2) | Salt | Cipher | Mac(32)]`です。AES鍵・IV・HMAC鍵をパスワードとソルトからPBKDF2で作り、MacはHMAC-SHA256でMagicから暗号文の終わりまでにかけます。復号の前に照合し、合わなければ`CryptographicException`を投げます。ヘッダの不正(短すぎる・Magicやバージョンが違う・ソルトの長さが不正)も同じ`CryptographicException`にして、利用者が「壊れたセーブ」を1つの例外で扱えるようにしています。CBCは暗号文を書き換えると平文も狙い通りに変わり、パディングさえ通れば復号に成功してしまうため、Macが無いと改ざんや破損に気付けません
- Magicを`SAC1`と分けているので、JSON版のファイルをMessagePack版で読むと(その逆も)、Magicの不一致で失敗します
- MessagePack版の圧縮は、シリアライザの`MessagePackCompression.Lz4BlockArray`で行います。暗号文はほとんど縮まないため、圧縮は必ず暗号化の前に行います

## HierarchyMessageBroker

- メッセージの型ごとに、`TypeIndex<T>`(ジェネリックの静的クラス)で0からの連番を振ります。ブローカーは連番を添字にした配列で購読者のリストを引くので、発行のたびに`Dictionary`を引きません
- 購読者の配列は、登録・解除のたびに作り直します(コピーオンライト)。発行は開始時点の配列を回すだけなので、発行中に登録・解除されても添字がずれず、発行のたびのコピーも要りません
- 同じデリゲートのインスタンスは、配列に1つだけ入れて、登録の回数を別の配列で数えます。デリゲートの`Equals`は「同じ対象・同じメソッド」なら別インスタンスでも等しいとみなすため、比較は参照で行います
- 配列の大きさは、アプリ全体で使われたメッセージの型の数に比例します(ブローカーごとに、そのブローカーが使う型の数ではなく、全体で何番目の型かに合わせて確保する)

## UpdateDispatcher

- `RuntimeInitializeLoadType.SubsystemRegistration`で、PlayerLoopの`Update`の末尾に`PlayerLoopSystem`を1つ差し込みます。前回のプレイで差し込んだものが残っていれば外してから差し込みます(ドメインリロードを無効にしている場合のため)
- 登録はリストと「要素 → 添字」の辞書で持ち、解除は末尾の要素を空いた位置に移して詰めます(定数時間)。回している最中の解除は、その位置を`null`にしておき、回し終えてから詰めます(まだ呼んでいない要素が前に移って飛ばされないため)
- 解除し忘れて破棄されたMonoBehaviourは、Unityの`== null`で検出して外します

## BackKeyReceiver

- 入力の監視は、内部の`BackKeyDispatcher`(`IUpdatable`)がまとめて行います。有効なReceiverが1つ以上あるときだけ`UpdateDispatcher`に登録します
- 押されたフレームには、全Receiverの判定(`CanInvoke`)を済ませてから、まとめて発火(`Invoke`)させます。判定と発火を交互に行うと、上の画面を閉じた直後に下の画面のボタンが最前面になり、同じフレームのうちに続けて発火してしまうためです
- Input Systemへの参照はasmdefの`versionDefines`で`SUPPLEMENT_INPUT_SYSTEM`を定義して切り替えます。Input Systemの無いプロジェクトでもコンパイルできます

## TapGuard

- ハンドルは構造体でコピーされうるため、ハンドルごとにIDを振り、有効なIDの集合で管理します。同じハンドル(とそのコピー)を何度破棄しても、解除は1回だけになります
