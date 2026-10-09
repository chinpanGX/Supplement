# コレクション

アセンブリ・名前空間: `Supplement.Core`(Unityに依存しない)

## EquatableReadOnlyList\<T\>

中身で比較できる読み取り専用のリストです。画面に渡すDTOに持たせ、「前回と同じなら描画し直さない」という判定に使います([RecycleRenderer](recycle-renderer.md)が使っています)。

```csharp
var items = dtos.ToEquatableReadOnlyList();
if (items == previousItems) return; // 同じ順序・同じ要素なら等しい
```

- 要素の型は`IEquatable<T>`を実装している必要があります(`record`は自動で実装されます)。参照で比べるクラスや、`IEquatable<T>`を持たない構造体を誤って使わないための制約です
- 比較するのは要素の1段目だけです。要素が配列やリストを持つ場合、そのメンバーの比較は要素の`Equals`次第です
- 作るときに要素をコピーするので、元のリストを後から変えても影響しません
- この型の変数で`foreach`したり`==`で比べたりしても、GCアロケーションは起きません。`IReadOnlyList<T>`として列挙すると列挙子がボックス化され、`==`も参照の比較になります

## Unityに依存しない一時リスト

`ScopedList<T>`と`RentedList<T>`は、要素を一時的に集めて使い終わったら返す、GCアロケーションを起こさないリストです。
どちらも`System.Buffers.ArrayPool<T>`だけで作っていて、**UnityEngineを参照しません**。Unityに依存しないアセンブリ(`noEngineReferences`のアセンブリ、サーバーと共有するロジック、ワーカースレッドで動かす処理)でも使えます。

### ListPool\<T\>との使い分け

Unityに依存するコードなら、`UnityEngine.Pool.ListPool<T>`でも同じことができます。

| | `ScopedList<T>` | `RentedList<T>` | `UnityEngine.Pool.ListPool<T>` |
|---|---|---|---|
| UnityEngineへの依存 | なし | なし | あり |
| `await`をまたげるか | できない(コンパイルエラー) | できる | できる |
| 返した後の誤用 | コンパイラが止める(フィールドに入れられない) | 止められない | 止められない |
| 使い終わった後のメモリ | 配列を`ArrayPool`に返す | 配列を`ArrayPool`に返す | 育ったリストを容量ごとプールに持ち続ける |
| 複数のスレッドから | 各スレッドで使える | 使える(プールはロックで守る) | ロックを取らないため、メインスレッドから使う |
| 使えるAPI | `Add`・インデクサ・`AsSpan`・`ToArray`・`Clear` | `Add`・インデクサ・`AsSpan`・`Clear`、`IReadOnlyList<T>` | `List<T>`のすべて |

- Unityに依存しないコードでは、`ScopedList`か`RentedList`を使います
- Unityに依存するコードで`List<T>`のAPIが必要なら、`ListPool<T>`で構いません
- メソッドの中で使い切るなら`ScopedList`を、`await`をまたぐなら`RentedList`を使います

## ScopedList\<T\>

`ArrayPool`の配列に要素を集める、1つのメソッドの中だけで使う一時リストです(`ref struct`)。毎フレーム呼ばれる処理などで、`new List<T>()`の代わりに使います。

```csharp
using var targets = new ScopedList<Enemy>();
foreach (var enemy in enemies)
{
    if (enemy.IsInRange(position)) targets.Add(enemy);
}
foreach (var target in targets) target.Damage(10);
```

- `Dispose`で配列をプールに返します。`using var`で受けてください
- 値渡しでコピーし、両方を`Dispose`すると、同じ配列を二重にプールへ返してしまいます

## RentedList\<T\>

`await`をまたいで使える一時リストです。インスタンスもプールしていて、`Rent`で取り出し、`Dispose`で配列ごと返します。

```csharp
using var ids = RentedList<string>.Rent();
foreach (var member in party) ids.Add(member.Id);
await connection.SendAsync(ids);
```

- `Dispose`の後のインスタンスは、次の`Rent`で使い回されます。`Dispose`した後に参照を持ち続けて使わないでください

## DisposableBag

複数の`IDisposable`をまとめて破棄します。`AddTo(bag)`で追加できます。破棄した後に追加したものは、その場で破棄します。
`Dispose`・`Clear`の途中で例外を投げたものがあっても、残りはすべて破棄してから例外を投げます(1件ならその例外を、複数なら`AggregateException`にまとめて)。
