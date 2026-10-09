# DIの補助

アセンブリ・名前空間: `Supplement.Unity`

各機能をDIコンテナに登録する方法は、それぞれの機能のページの「DIへの登録」に書いています。
このページには、DIコンテナを使う場面で補助的に使う型をまとめます。

## ObjectResolverGateway

`IObjectResolver`に静的にアクセスするための入り口です。コンストラクタインジェクションも`[Inject]`も使えない場所から、例外的に依存を取り出すときだけ使います。

```csharp
ObjectResolverGateway.Register(container);   // 起動時に1回
var broker = ObjectResolverGateway.Resolve<IMessageBroker>();
if (ObjectResolverGateway.TryResolve<IMessageBroker>(out var b)) { ... }  // 未登録ならfalse
ObjectResolverGateway.Reset();               // 登録し直す前に呼ぶ
```

- `Register`は、登録済みのまま呼ぶと`InvalidOperationException`になります。登録し直すときは先に`Reset`を呼びます
- `Resolve`は、`Register`の前に呼ぶと`InvalidOperationException`になります。`TryResolve`は`false`を返します

通常はDIコンテナからの注入を優先してください。

登録はプレイの開始時(`SubsystemRegistration`)に消えます。ドメインリロードを無効にしていても、前のプレイの登録は残りません。

## ComponentExtensions

- `GetOrAddComponent<T>()`: 付いていれば取得し、無ければ追加する
- `Component.SetActive(bool)`: `gameObject.SetActive`の省略形
