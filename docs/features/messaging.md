# メッセージング

メッセージ(構造体)を発行して、購読者に届けます。届く範囲によって2種類あります。

| 型 | 届く範囲 | アセンブリ |
|---|---|---|
| `IMessageBroker` | アプリ全体 | `Supplement.Core`(抽象) |
| `GlobalMessageBroker` | アプリ全体(`IMessageBroker`のZeroMessengerによる実装) | `Supplement.ZeroMessenger`(別パッケージ) |
| `HierarchyMessageBroker` | 自身より下の階層のコンポーネントからだけ | `Supplement.Unity` |

メッセージの型は構造体(`where T : struct`)に限ります。

## DIへの登録

- `IMessageBroker`: 登録用の拡張メソッドはありません。実装を直接登録します

  ```csharp
  builder.Register<IMessageBroker, GlobalMessageBroker>(Lifetime.Singleton);
  ```

- `HierarchyMessageBroker`: DIへの登録は要りません。受け取る側のGameObjectにコンポーネントとして付けます

## IMessageBroker(アプリ全体)

どの画面から発行されるか決まっていない通知(所持数の変化など)に使います。

```csharp
// 購読(戻り値をDisposeすると解除)
messageBroker.Subscribe<ItemAmountChanged>(x => Refresh(x.ItemId)).AddTo(disposableBag);

// 発行
messageBroker.Publish(new ItemAmountChanged { ItemId = 1001 });
```

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
