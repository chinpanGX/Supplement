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

## MessageBroker

- `HierarchyMessageBroker`と同じく、型ごとの連番(`TypeIndex<T>`)を添字にした配列で、型ごとのチャンネル(内部クラスの`MessageChannel<T>`)を引きます。チャンネルは、購読者(内部クラスの`MessageSubscriber<T>`)自身をノードにした双方向リストで購読者を持ちます。登録・解除は`lock`の中で前後をつなぎ替えるだけで、購読者の数によらず定数時間です。確保も購読者1つだけです。配列をコピーオンライトで作り直していたときは、購読者の数に比例して時間とメモリが増えていました([ベンチマーク](benchmark.md))
- 発行は`lock`を取らずにリストを辿ります。どのスレッドからも呼べるようにしつつ、発行を速くするためです
  - 同期のハンドラーの例外は、`UniTask.FromException(e).Forget()`で`UniTaskScheduler.UnobservedTaskException`に流します。非同期のハンドラーの例外と同じ道を通すためです(`Supplement.Core`はUnityに依存しないので、ログを直接は出しません)
  - 購読者を辿るループは、`try/catch`の無い別のメソッド(`InvokeFrom`)に分け、埋め込みを禁じています。同じメソッドに`try/catch`とループがあると、`catch`でも使うノードの参照をJITがレジスタに置けず、次のノードへ進むたびにスタックへの書き込みと読み出しが挟まるためです。例外が出たら、先頭から辿り直し、例外を投げた購読者より版が新しい最初の購読者から`InvokeFrom`を呼び直します。どこまで呼んだかは、ノードの参照ではなく版(`long`)で控えます。参照を書くと、ノードごとにGCの書き込みバリアが入るためです
  - 同期の購読者は、ハンドラーのデリゲートをノードの`SyncHandler`に持ち、発行は仮想呼び出しを通さずにそれを直接呼びます。非同期の購読者は`null`なので、`Invoke`を呼びます
  - 発行中に登録された購読者に届けないため、チャンネルは版(`version`)を持ちます。登録のたびに版を進めてノードに記録し、発行は開始時の版より新しいノードに着いたら止めます。末尾に追加するので、それより後ろもすべて新しいノードです。版は`long`で、`Volatile`で読み書きして64bitでも不可分にします。登録では版を進めてからノードをつなぐので、発行が新しいノードを見つけたときには、必ずその版より古い版を控えています
  - 解除したノードは、次のノードへの参照(`Next`)を残します。発行がちょうどそのノードを辿っていても、先へ進めるようにするためです。前のノードへの参照(`Previous`)は発行で使わないので外します
  - 先頭のノードの`Previous`は末尾のノードを指します。末尾への追加を、リストを辿らずに定数時間で行うためです
  - 解除したノードが`Next`を残すため、利用者が解除済みの`IDisposable`を持ち続けると、その先のノードもGCされずに残ることがあります。辿っている途中の発行を壊さずに`Next`を外す手段が無いため、許容しています(R3の`ObserverNode`も同じ作りです)
- 購読者(内部クラスの`MessageSubscriber<T>`)が、購読を解除する`IDisposable`も兼ねます。購読のたびの確保を1つにするためです。購読者は解除済みの印を持ち、発行は解除済みの購読者を飛ばします。発行中に解除された購読者を、その発行で呼ばないためです。解除済みの印は、チャンネルの`lock`の中で立てます。解除はどのみち`lock`を取るので、`Interlocked`で別に立てるよりアトミックな操作が1回少なく済みます(購読と解除で`lock`の2回だけ)
- 非同期の購読者は、戦略ごとのクラスに分けています
  - `Sequential`: メッセージをキューに入れ、1つのループ(`Pump`)が順に処理します。ループが動いていなければ、入れた側が開始します。ハンドラーが完了していなければ、完了を待ってから続きを処理します(`ContinuePumpAsync`)。キューにはメッセージだけを入れるので、同期的に完了する間は確保しません
  - `Switch`: メッセージごとに`CancellationTokenSource`を作り、前のものを取り消します。処理の終了と取り消しが重なると、破棄済みの`CancellationTokenSource`を取り消して例外になるため、`Dispose`はしません(タイマーもWaitHandleも使わないので、破棄しなくても漏れません)
  - `Drop`: 処理中の印を`Interlocked.CompareExchange`で立て、立っていれば捨てます
- 購読の解除で取り消すトークンは、購読者ごとの`CancellationTokenSource`です。ハンドラーにはこのトークン(`Switch`ではメッセージごとのトークン)をそのまま渡します
- 完了を待つ発行(`PublishAsync`)は持ちません。発行する側が購読者の完了に依存するのはpub/subの前提に合わないためです。持っていたときは、完了を知らせるための確保が購読者ごとに起き、発行のトークンと購読の寿命のトークンをつなぐ確保も避けられませんでした
- `MessageBroker.Default`は静的なので、ドメインリロードを無効にすると前回のプレイの購読が残ります。`Supplement.Unity`の`MessageBrokerDefaultReset`が、`RuntimeInitializeLoadType.SubsystemRegistration`で内部メソッドの`UnsubscribeAll`を呼んで消します(`Supplement.Core`はUnityに依存しないため、`InternalsVisibleTo`で`Supplement.Unity`から呼びます)

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
