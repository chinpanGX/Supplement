# ベンチマーク(メンテナー向け)

[メッセージング](../features/messaging.md)の`MessageBroker`を、ZeroMessengerと比べます。機能を絞っても、速さとGCアロケーションで劣らないことを確かめるためです。

ベンチマークはリポジトリ直下の`Benchmarks/`にあります(.NET 10のコンソールアプリ・BenchmarkDotNet)。Unityプロジェクトのソースをリンクしているので、ソースを直せばそのまま反映されます。

## 回し方

```powershell
$env:PATH = "C:\Program Files\dotnet;$env:PATH"   # .NET 10のSDKを先に見つけさせる
cd Benchmarks\Supplement.Benchmarks

dotnet run -c Release -- --filter * --launchCount 3 --exporters github   # 結果に使うのはこれ
dotnet run -c Release -- --filter * --job dry                            # 動くかだけ確かめる
```

- 重いアプリ(ブラウザ・Unity)は閉じて測ります。結果は`BenchmarkDotNet.Artifacts/results/`に出ます
- Git Bashでは`--filter '*'`のように引用符で囲みます

## 結果(2026-10-17)

.NETでの相対的な比較です。Unity(Mono・IL2CPP)上の速さの絶対値には使えません。

| 計測 | MessageBroker | ZeroMessenger |
|---|---:|---:|
| Publish・同期のハンドラー・購読者1人 | 5.4 ns | 26.9 ns |
| Publish・同期のハンドラー・購読者10人 | 21.4 ns | 30.7 ns |
| Publish・同期のハンドラー・購読者100人 | 188.0 ns | 190.5 ns |
| 購読して解除(既存の購読者100人) | 32.6 ns / 64 B | 28.9 ns / 64 B |

`SubscribeAwait`の購読者10人への`Publish`(「待つ」はハンドラーが本当に待つ場合):

| ストラテジー | MessageBroker | ZeroMessenger | MessageBroker(待つ) | ZeroMessenger(待つ) |
|---|---:|---:|---:|---:|
| Sequential | 406 ns / 0 B | 438 ns / 0 B | 778 ns / 0 B | 1,400 ns / 2,640 B |
| Parallel | 34 ns / 0 B | 102 ns / 0 B | 386 ns / 0 B | 594 ns / 1,280 B |
| Switch | 296 ns / 480 B | 771 ns / 2,560 B | 663 ns / 480 B | 1,236 ns / 3,840 B |
| Drop | 70 ns / 0 B | 267 ns / 0 B | 435 ns / 0 B | 1,268 ns / 2,560 B |

(時間 / 1回あたりのGCアロケーション。同期のPublishはどちらも0 B)

- 発行は、すべての条件で`MessageBroker`が同じか速く、GCアロケーションも少なくなります
- 購読と解除だけは、ZeroMessengerが1割ほど速くなります
