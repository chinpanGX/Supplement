# 起動時の初期化

アセンブリ・名前空間: `Supplement.Core`

起動時に必要な初期化(マスターデータの読み込み、サインインなど)を、優先度の順に実行します。

## 主な型

| 型 | 役割 |
|---|---|
| `IBootInitializationTask` | 初期化の処理1つ。`Priority`と`InitializeAsync(CancellationToken)`を実装する |
| `InitializationPriority` | 実行順。`Highest`(0)〜`Lowest`(600)。値の小さいものから実行する |
| `BootInitializer` | 登録されたタスクを優先度の順に実行する |

## 動き

- 優先度の小さい順に実行します。**同じ優先度のタスクは並列に**実行します
- いずれかのタスクが失敗すると、それより後の優先度のタスクは実行せず、その例外を投げます
- `RunAsync`は1回しか呼べません(2回目は`InvalidOperationException`)
- `BootInitializer`自体はエントリーポイントではありません。アプリ側のエントリーポイントから`RunAsync`を呼び、初期化の後の処理はその後に続けて書きます

## DIへの登録

`IContainerBuilder`(VContainer)の拡張メソッド`RegisterBootInitializer()`で、`BootInitializer`をSingletonとして登録します。
初期化のタスクは、アプリ側で`As<IBootInitializationTask>()`を付けて登録します。`BootInitializer`は登録されたタスクをすべて受け取ります。

## 使い方

```csharp
public sealed class MasterDataInitializationTask : IBootInitializationTask
{
    public InitializationPriority Priority => InitializationPriority.High;

    public async UniTask InitializeAsync(CancellationToken ct)
    {
        // マスターデータを読み込む
    }
}

// LifetimeScope
builder.RegisterBootInitializer();
builder.Register<MasterDataInitializationTask>(Lifetime.Singleton).As<IBootInitializationTask>();
builder.RegisterEntryPoint<BootstrapEntryPoint>();

// エントリーポイント
public sealed class BootstrapEntryPoint : IAsyncStartable
{
    private readonly BootInitializer bootInitializer;

    public BootstrapEntryPoint(BootInitializer bootInitializer)
    {
        this.bootInitializer = bootInitializer;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        await bootInitializer.RunAsync(cancellation);
        // 最初の画面へ進む
    }
}
```
