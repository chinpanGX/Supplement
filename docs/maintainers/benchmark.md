# ベンチマーク(メンテナー向け)

[メッセージング](../features/messaging.md)の`MessageBroker`を、ZeroMessengerと比べるベンチマークです。`MessageBroker`は機能を最小限に絞っています。機能を絞っても速さとGCアロケーションで劣らないことを、数値で確かめるために置いています。

ベンチマークはリポジトリ直下の`Benchmarks/`にあります。Assetsの外なので、Unityの管理対象外で、パッケージにも含まれません。

## 構成

- `.NET 10`のコンソールアプリで、[BenchmarkDotNet](https://benchmarkdotnet.org/)で測ります
- NuGetで入れるのは`BenchmarkDotNet`・`UniTask`・`ZeroMessenger`の3つです
- 測る実装は、Unityプロジェクトのソース(`IMessageBroker`・`AsyncPublishStrategy`・`AsyncSubscribeStrategy`・`Messaging/`・`GlobalMessageBroker`)をコピーせずにリンクしています。ソースを直せば、そのままベンチマークに反映されます

| クラス | 測るもの |
|---|---|
| `PublishBenchmark` | 同期のハンドラーへの`Publish`。購読者が0・1・10・100のとき。基準はC#の`event` |
| `AsyncPublishBenchmark` | `SubscribeAwait`の購読者への`Publish`(ハンドラーは同期的に完了する)。両方にある`Sequential`と`Parallel`を、購読者が1・10のときで比べる |
| `SubscribeBenchmark` | 購読して、すぐに解除する。計測の前から登録されている購読者が0・100のとき |

## 回し方

`global.json`で.NET 10のSDKを指定しています。PATHで先に見つかる`dotnet`が古いSDKだと、BenchmarkDotNetが起動に失敗します(`NullReferenceException`)。その場合は、.NET 10を入れた場所をPATHの先頭に置きます。

```powershell
$env:PATH = "C:\Program Files\dotnet;$env:PATH"
cd Benchmarks\Supplement.Benchmarks

dotnet run -c Release -- --filter *                       # すべて(10〜15分ほど)
dotnet run -c Release -- --filter *PublishBenchmark*      # 一部だけ
dotnet run -c Release -- --filter * --job dry             # 動くかだけ確かめる(数値は使えない)
dotnet run -c Release -- --filter * --exporters github    # GitHubに貼れるMarkdownも出す
```

- Git Bashでは`*`がファイル名に展開されるので、`--filter '*'`のように引用符で囲みます
- 結果は`BenchmarkDotNet.Artifacts/results/`に出ます(git管理外)
- ブラウザやUnityなど重いアプリは閉じて測ります
- Riderでは`Benchmarks/Supplement.Benchmarks.sln`を開き、設定の「.NET CLI executable path」に.NET 10の`dotnet.exe`を指定します

## 結果

2026-10-14に測った結果です。

- CPU: Intel Core i7-13700F / メモリ: 16GB / OS: Windows 11
- .NET 10.0.12(X64 RyuJIT AVX2)/ BenchmarkDotNet 0.15.2 / UniTask 2.5.10 / ZeroMessenger 1.0.4

**これは.NETでの相対的な比較です。** UnityはMono(エディタ・Monoのプレイヤー)かIL2CPPで動き、.NET 10のCoreCLRとは別物です。Unity上の速さの絶対値には使えません。

### Publish(同期のハンドラー)

| 購読者 | C# event | MessageBroker | ZeroMessenger | GlobalMessageBroker |
|---:|---:|---:|---:|---:|
| 0 | 0.0 ns | 0.0 ns | 26.1 ns | 26.5 ns |
| 1 | 1.1 ns | 2.8 ns | 26.0 ns | 26.3 ns |
| 10 | 19.3 ns | 20.9 ns | 29.7 ns | 30.0 ns |
| 100 | 184.4 ns | 204.9 ns | 189.5 ns | 172.4 ns |

どの組み合わせもGCアロケーションは0です。

### Publish(非同期の購読者)

| ストラテジー | 購読者 | MessageBroker | ZeroMessenger |
|---|---:|---:|---:|
| Sequential | 1 | 40.6 ns | 65.7 ns |
| Sequential | 10 | 401.8 ns | 436.2 ns |
| Parallel | 1 | 5.3 ns | 32.8 ns |
| Parallel | 10 | 39.6 ns | 98.3 ns |

どの組み合わせもGCアロケーションは0です。

### 購読と解除

| 既存の購読者 | C# event | MessageBroker | ZeroMessenger | GlobalMessageBroker |
|---:|---:|---:|---:|---:|
| 0 | 6.2 ns / 0 B | 33.9 ns / 72 B | 28.4 ns / 64 B | 70.5 ns / 104 B |
| 100 | 107.2 ns / 1,176 B | 133.1 ns / 1,696 B | 28.8 ns / 64 B | 72.6 ns / 104 B |

(時間 / 1回あたりのGCアロケーション)

### 読み方

- **発行は、購読者が少ないほど差が大きくなります。** ZeroMessengerは購読者の数によらず25ns前後の固定のコストがあり、`MessageBroker`にはありません。購読者が1人なら約9倍、10人なら約1.4倍速くなります。100人では、購読者ごとのコストが効いてZeroMessengerがやや速くなります(1割以内)
- **非同期の購読者への発行は、すべての条件で`MessageBroker`が速くなりました。** 同期的に完了するハンドラーで`async`メソッドを通さないためです([パフォーマンスとGC](performance.md))
- **購読と解除はZeroMessengerが速く、購読者が多いほど差が開きます。** `MessageBroker`は購読者の配列を登録・解除のたびに作り直すため、既存の購読者の数に比例して時間とアロケーションが増えます。発行のたびのコピーやロックを無くすための、意図した設計です([内部の設計](internals.md))。購読と解除は画面を開くとき程度の頻度で、毎フレームの発行より少ない前提です
- 購読者が0のとき、`MessageBroker`は型ごとのチャンネルがまだ無いため、ほぼ何もしません
- 100人のときに`GlobalMessageBroker`がZeroMessengerを直接使うより速く出ていますが、ラッパーの分だけ遅くなることはあっても速くなる理由は無いので、計測のぶれと見ています
