# 多重実行の防止(TapGuard)

アセンブリ・名前空間: `Supplement.Core`

非同期処理の実行中に同じ処理がもう一度始まらないようにする、再入ガードです。ボタンの連打対策などに使います。

```csharp
private readonly TapGuard tapGuard = new();

private async UniTaskVoid OnClickAsync()
{
    if (tapGuard.IsGuarding) return;

    using (tapGuard.BeginGuard())
    {
        await SendAsync();
    }
}
```

- `BeginGuard`の戻り値(`TapGuard.GuardHandle`)を破棄するとガードが終わります
- ネストして呼んだ場合は、すべてのハンドルを破棄するまでガードが続きます
- 同じハンドル(とそのコピー)を何度破棄しても、解除は1回だけです
- ハンドルは構造体なので、`using`や`GuardHandle`型の変数で受ければGCアロケーションは起きません。`IDisposable`型の変数で受けるとボックス化します
- UIには依存しません。Buttonの`interactable`も切り替えたい場合は、呼び出し側が`IsGuarding`を見て制御します
