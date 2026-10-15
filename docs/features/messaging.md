# メッセージング

メッセージ(構造体)を発行して、購読者に届けます。届く範囲によって2種類あります。

| 型                        | 届く範囲                                       | アセンブリ                              |
| ------------------------ | ------------------------------------------ | ---------------------------------- |
| `IMessageBroker`         | アプリ全体                                      | `Supplement.Core`(抽象)              |
| `MessageBroker`          | アプリ全体(`IMessageBroker`の実装。外部ライブラリに依存しない)   | `Supplement.Core`                  |
| `GlobalMessageBroker`    | アプリ全体(`IMessageBroker`のZeroMessengerによる実装) | `Supplement.ZeroMessenger`(別パッケージ) |
| `HierarchyMessageBroker` | 自身より下の階層のコンポーネントからだけ                       | `Supplement.Unity`                 |

メッセージの型は構造体(`where T : struct`)に限ります。

## DIへの登録

- `IMessageBroker`: `RegisterMessageBroker()`で、アプリ全体で共有する`MessageBroker.Default`を登録します

  ```csharp
  using Supplement.Unity;

  builder.RegisterMessageBroker();
  ```

  ZeroMessengerの実装を使う場合は、拡張メソッドは無いので直接登録します

  ```csharp
  builder.Register<IMessageBroker, GlobalMessageBroker>(Lifetime.Singleton);
  ```

- `HierarchyMessageBroker`: DIへの登録は要りません。受け取る側のGameObjectにコンポーネントとして付けます

## IMessageBroker(アプリ全体)

どの画面から発行されるか決まっていない通知(所持数の変化など)に使います。DIを使わない場所からは、`MessageBroker.Default`をそのまま使えます。

```csharp
// 購読(戻り値をDisposeすると解除)
messageBroker.Subscribe<ItemAmountChanged>(x => Refresh(x.ItemId)).AddTo(disposableBag);

// 発行
messageBroker.Publish(new ItemAmountChanged { ItemId = 1001 });

// DIを使わない場合
MessageBroker.Default.Publish(new ItemAmountChanged { ItemId = 1001 });
```

### 非同期の購読

`SubscribeAwait`で、非同期の処理で購読できます。処理中に次のメッセージが届いたときの扱いを`AsyncSubscribeStrategy`で選びます(省略すると`Sequential`)。

```csharp
messageBroker.SubscribeAwait<ItemAmountChanged>(async (x, ct) =>
{
    await PlayCountUpAsync(x.ItemId, ct);
}, AsyncSubscribeStrategy.Switch).AddTo(disposableBag);
```

| `AsyncSubscribeStrategy` | 処理中に次のメッセージが届いたとき |
|---|---|
| `Sequential` | 届いた順に並べ、前の処理が終わってから次を始める |
| `Parallel` | 前の処理を待たずに、次を始める |
| `Switch` | 処理中のハンドラーに渡したトークンを取り消し、次を始める |
| `Drop` | 届いたメッセージを捨てる |

発行して、ハンドラーの完了を待つ方法はありません。発行する側は購読者を知らない前提だからです。ほかの処理の完了を待ってから続けたい場合は、メッセージではなく、その処理を直接呼んで待ってください。

### 動き(MessageBroker)

- `Publish`は、同期のハンドラーをその場で登録順に呼びます。非同期のハンドラーは開始するだけで、完了を待ちません
- 購読者が例外を投げても残りの購読者は呼びます。例外は`Publish`の呼び出し元には投げず、UniTaskの`UniTaskScheduler.UnobservedTaskException`に流します(既定ではログに出ます)。同期のハンドラーも非同期のハンドラーも同じです。購読者の不具合で、発行した側の処理が止まらないようにするためです。`OperationCanceledException`は、UniTaskの既定(`UniTaskScheduler.PropagateOperationCanceledException`が`false`)では流れません
- `Subscribe`・`SubscribeAwait`は`IDisposable`を返します。破棄すると購読を解除し、処理中の非同期のハンドラーに渡したトークンを取り消します。`Sequential`でまだ始まっていないメッセージは呼ばずに捨てます
- 購読の解除・`Switch`での切り替えで取り消されたハンドラーの`OperationCanceledException`は、例外として扱いません
- 範囲外の`AsyncSubscribeStrategy`を渡すと、その場で`ArgumentOutOfRangeException`を投げます
- 同じデリゲートを重ねて登録すると、登録した回数だけ呼びます
- 発行中に登録しても、その発行には届きません(次の発行から届きます)。発行中に解除した購読者には、その発行でもそれ以降は届けません
- 発行・購読・解除は、どのスレッドから呼んでも構いません
- 同期のハンドラーへの`Publish`はGCアロケーションを起こしません。同期的に完了する非同期のハンドラー(`Sequential`・`Parallel`・`Drop`)も同じです。`Switch`はメッセージごとに`CancellationTokenSource`を作ります
- ブローカーを`Dispose`すると、購読をすべて解除し、以後の`Publish`・`Subscribe`などは`ObjectDisposedException`を投げます

### 注意

- `MessageBroker.Default`は`Dispose`しないでください。アプリ全体で使えなくなります。`RegisterMessageBroker()`は`RegisterInstance`で登録するので、コンテナを破棄しても`Default`は破棄されません
- `Default`の購読は、解除するまでアプリの終了まで残ります。画面やオブジェクトより長く生きるため、購読者の破棄のタイミングで必ず解除してください
- プレイモードに入るときのドメインリロードを無効にしていても、プレイの開始時に`Default`の購読はすべて解除されます(前回のプレイの購読は残りません)
- `GlobalMessageBroker`は、ハンドラーの例外をZeroMessengerの扱いのまま返します。同期のハンドラーの例外は`Publish`の呼び出し元にそのまま投げ、残りの購読者は呼びません
- `GlobalMessageBroker`を破棄すると、そのインスタンスを通じた購読だけを解除します。ZeroMessengerの`MessageBroker<T>.Default`を直接使った購読は解除しません

## HierarchyMessageBroker(ヒエラルキーの中だけ)

一覧の要素(子のGameObject)から、一覧の親へ通知するような場面に使います。子は親を参照する必要がありません。

```csharp
// 親(一覧のView)
var broker = this.GetOrAddComponent<HierarchyMessageBroker>();
subscription = broker.Subscribe<IncrementItemAmountMessage>(x => presenter.AddItemAmount(x.ItemId));

// 子(一覧の要素)。親を辿って最初に見つかったブローカーに届く
HierarchyMessageBroker.Publish(this, new IncrementItemAmountMessage { ItemId = id });
```

- `Publish(Component, T)`は、そのコンポーネントのGameObjectから親へ辿って、最初に見つかったブローカーに届けます(非アクティブなGameObjectのブローカーも見つけます)。見つからなければ何もしません
- `Subscribe`は`IDisposable`を返します。破棄すると購読を解除します。子より長生きする購読者は、破棄のタイミングで解除してください
- 同じデリゲートのインスタンスを重ねて登録しても、発行時に呼ぶのは1回だけです。登録の回数を数えているので、すべての戻り値を破棄するまで登録は残ります
- 購読者は登録順に呼びます。購読者が例外を投げても残りの購読者は呼び、最後にまとめて`AggregateException`で投げます
- 発行中に登録・解除しても、その発行は開始時点の購読者に届けます。変更は次の発行から反映されます
- `Publish(Component, T)`は呼ぶたびに親を辿ってブローカーを探します。頻繁に発行する場合は、ブローカーを持っておいてインスタンスの`Publish(T)`を呼びます。発行そのものはGCアロケーションを起こしません
