# メッセージング

メッセージ(構造体)を発行して、購読者に届けます。

| 型 | 届く範囲 | アセンブリ |
|---|---|---|
| `IMessageBroker` / `MessageBroker` | アプリ全体 | `Supplement.Core` |
| `HierarchyMessageBroker` | 自身より下の階層のコンポーネントからだけ | `Supplement.Unity` |

メッセージの型は構造体(`where T : struct`)に限ります。

## MessageBroker(アプリ全体)

どの画面から発行されるか決まっていない通知(所持数の変化など)に使います。

### DIへの登録

```csharp
using Supplement.Unity;

builder.RegisterMessageBroker(); // IMessageBrokerとしてMessageBroker.Defaultを登録する
```

DIを使わない場所からは、`MessageBroker.Default`をそのまま使えます。

### 使い方

```csharp
// 購読(戻り値をDisposeすると解除)
messageBroker.Subscribe<ItemAmountChanged>(x => Refresh(x.ItemId)).AddTo(disposableBag);

// 発行
messageBroker.Publish(new ItemAmountChanged { ItemId = 1001 });

// 非同期の購読
messageBroker.SubscribeAwait<ItemAmountChanged>(async (x, ct) =>
{
    await PlayCountUpAsync(x.ItemId, ct);
}, AsyncSubscribeStrategy.Switch).AddTo(disposableBag);

// 次に発行されたメッセージを1件だけ待つ
var message = await messageBroker.FirstAsync<TutorialStepCompleted>(destroyCancellationToken);
```

`SubscribeAwait`は、処理中に次のメッセージが届いたときの扱いを選べます(省略すると`Sequential`)。

| `AsyncSubscribeStrategy` | 処理中に次のメッセージが届いたとき |
|---|---|
| `Sequential` | 届いた順に並べ、前の処理が終わってから次を始める |
| `Parallel` | 前の処理を待たずに、次を始める |
| `Switch` | 処理中のハンドラーのトークンを取り消し、次を始める |
| `Drop` | 届いたメッセージを捨てる |
| `ThrottleFirstLast` | 最後の1件だけ残し、前の処理が終わってからそれを処理する |

### 動き

- `Publish`は、ハンドラーを登録順にその場で呼びます。非同期のハンドラーは開始するだけで、完了を待ちません
- ハンドラーの例外は`Publish`の呼び出し元に投げず、`UniTaskScheduler.UnobservedTaskException`に流します(既定ではログに出ます)。残りの購読者は呼ばれます
- 購読を解除すると、処理中の非同期のハンドラーのトークンを取り消します。取り消しによる`OperationCanceledException`は例外として扱いません
- 発行中に登録した購読者は、次の発行から届きます。発行中に解除した購読者には、その発行でも届きません
- 同じデリゲートを重ねて登録すると、登録した回数だけ呼びます
- どのスレッドから呼んでも構いません
- 同期のハンドラーと、同期的に完了する非同期のハンドラーへの発行は、GCアロケーションを起こしません(`Switch`だけ、メッセージごとに`CancellationTokenSource`を作ります)
- ハンドラーが`null`なら`ArgumentNullException`、範囲外の`AsyncSubscribeStrategy`なら`ArgumentOutOfRangeException`を投げます。`Dispose`したブローカーは、以後の発行・購読で`ObjectDisposedException`を投げます

### 注意

- 発行して、ハンドラーの完了を待つ方法はありません。待ちたい処理は、メッセージではなく直接呼んでください
- `FirstAsync`は、それより前に発行されたメッセージを受け取りません。ブローカーの破棄では完了しないので、トークンで取り消してください
- `MessageBroker.Default`は`Dispose`しないでください。アプリ全体で使えなくなります(`RegisterMessageBroker()`で登録したものは、コンテナを破棄しても破棄されません)
- `Default`の購読は、解除するまでアプリの終了まで残ります。購読した側の破棄のタイミングで必ず解除してください。プレイモードの開始時には、前回のプレイの購読はすべて解除されます

## HierarchyMessageBroker(ヒエラルキーの中だけ)

一覧の要素(子のGameObject)から、一覧の親へ通知するような場面に使います。子は親を参照する必要がありません。DIへの登録は要らず、受け取る側のGameObjectにコンポーネントとして付けます。

```csharp
// 親(一覧のView)
var broker = this.GetOrAddComponent<HierarchyMessageBroker>();
subscription = broker.Subscribe<IncrementItemAmountMessage>(x => presenter.AddItemAmount(x.ItemId));

// 子(一覧の要素)。親を辿って最初に見つかったブローカーに届く
HierarchyMessageBroker.Publish(this, new IncrementItemAmountMessage { ItemId = id });
```

- `Subscribe`・`SubscribeAwait`・`FirstAsync`と発行の動きは、`MessageBroker`と同じです
- `Publish(Component, T)`は親へ辿って、最初に見つかったブローカー(非アクティブでも)に届けます。見つからなければ何もしません。呼ぶたびに親を辿るので、頻繁に発行するならブローカーを持っておいて`Publish(T)`を呼びます
- GameObjectが破棄されると、購読をすべて解除します。破棄の後に発行しても何も起きません。ただし、一度もアクティブにならなかったGameObjectは`OnDestroy`が呼ばれないため、解除されません
- `FirstAsync`は、GameObjectの破棄では完了しません。`destroyCancellationToken`などで取り消してください

## GlobalMessageBroker(非推奨)

ZeroMessengerで実装した`IMessageBroker`です(別パッケージ`Supplement.ZeroMessenger`)。今後のバージョンで削除します。`RegisterMessageBroker()`に置き換えてください。

```csharp
builder.Register<IMessageBroker, GlobalMessageBroker>(Lifetime.Singleton);
```

`MessageBroker`との違い:

- 同期のハンドラーの例外を、`Publish`の呼び出し元にそのまま投げます。残りの購読者は呼びません
- `ThrottleFirstLast`には対応していません(`NotSupportedException`を投げます)
- 破棄すると、そのインスタンスを通じた購読だけを解除します。ZeroMessengerの`MessageBroker<T>.Default`を直接使った購読は解除しません
