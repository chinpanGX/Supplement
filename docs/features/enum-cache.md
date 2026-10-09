# 列挙型のキャッシュ(EnumCache)

アセンブリ・名前空間: `Supplement.Core`

列挙型の値と名前を、型ごとにキャッシュします。`Enum.ToString()`・`Enum.GetValues`・`Enum.IsDefined`は、呼ぶたびに文字列や配列を作ったりボックス化したりします。毎フレームや一覧の表示のたびに呼ぶ場面では、その代わりに使います。

## 主なメンバー

| メンバー | 内容 |
|---|---|
| `Values` | 定義された値の一覧(`ReadOnlySpan<T>`)。`Enum.GetValues`と同じ、値を符号なしとみなした順(負の値は正の値より後ろ) |
| `Names` | 定義された名前の一覧(`ReadOnlySpan<string>`)。`Values`と同じ並び |
| `GetName(T)` | 値の名前。定義済みの値ならキャッシュした文字列を返す |
| `IsDefined(T)` | 定義済みの値か |

## 使い方

```csharp
foreach (var type in EnumCache<PachimonType>.Values)
{
    label.text = EnumCache<PachimonType>.GetName(type);
}

if (!EnumCache<PachimonType>.IsDefined(value))
{
    throw new ArgumentOutOfRangeException(nameof(value));
}
```

## 動き

- キャッシュは、その列挙型を最初に使ったときに1回だけ作ります
- 定義済みの値に対する`GetName`・`IsDefined`は、GCアロケーションを起こしません
- 定義されていない値(Flagsの組み合わせなど)の`GetName`は`ToString()`に任せるため、そのときだけ文字列を作ります
- 同じ値に複数の名前がある場合は、先に並ぶ名前を返します

## 注意

- `Values`・`Names`は`ReadOnlySpan`なので、`async`メソッドの中では`foreach`で回せません(C# 9の制約)。その場合は`for`と添字で回します
