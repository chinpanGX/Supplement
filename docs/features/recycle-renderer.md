# リストの描画(RecycleRenderer)

アセンブリ・名前空間: `Supplement.Unity`(`IAsyncRenderable<T>`だけ`Supplement.Core`)

DTOの配列を受け取り、要素ごとにGameObjectを用意して描画します。GameObjectは`ObjectPool`で使い回すため、件数が変わっても作り直しを最小限にできます。

## 主な型

| 型 | 役割 |
|---|---|
| `IAsyncRenderable<TDto>` | 要素のコンポーネントが実装する。`RenderAsync(TDto)`で1件を描画する |
| `RecycleRenderer<TRenderable, TDto>` | 配列を描画する本体(ピュアC#) |
| `RecycleRendererFactory` | テンプレートと配置先をInspectorで設定し、`RecycleRenderer`を作るコンポーネント |

`RecycleRenderer`はジェネリックなのでAdd Componentできないため、`RecycleRendererFactory`を経由して作ります。

## 使い方

```csharp
public sealed class ItemElement : MonoBehaviour, IAsyncRenderable<ItemDto>
{
    public UniTask RenderAsync(ItemDto dto) { ... }
}

public sealed class ItemListView : MonoBehaviour
{
    [SerializeField] private RecycleRendererFactory factory;
    private RecycleRenderer<ItemElement, ItemDto> renderer;

    private void Awake()
    {
        renderer = factory.Create<ItemElement, ItemDto>();
    }

    public UniTask RenderAsync(EquatableReadOnlyList<ItemDto> items) => renderer.RenderAsync(items);
}
```

## 動き

- テンプレートのGameObjectは、`Create`したときに非アクティブにします
- 前回と同じ内容([EquatableReadOnlyList](collections.md)で比較)なら何もしません。前回の描画が途中で失敗していた場合は、同じ内容でも描画し直します
- 件数が減った分はプールに戻し(非アクティブにする)、増えた分はプールから取り出します
- 並び順はヒエラルキーの順番(`SetSiblingIndex`)に揃えます
- 全要素の`RenderAsync`を並列に呼び、すべて終わるまで待ちます
- 要素はDIコンテナ(`IObjectResolver.Instantiate`)で作るので、要素のコンポーネントにも`[Inject]`が効きます。そのため`RecycleRendererFactory`はDIコンテナ経由で生成されている必要があります

## 注意

- 前回の`RenderAsync`が終わる前に、次の`RenderAsync`を呼ばないでください
- DTOの型は`class`で、`IEquatable<TDto>`を実装している必要があります(`record`を推奨)
- `RecycleRendererFactory`1つにつき、`Create`できるのは1回だけです(2回目と、DIコンテナから注入される前の`Create`は`InvalidOperationException`)。作った`RecycleRenderer`は、Factoryの破棄時に一緒に破棄されます。破棄した後の`RenderAsync`は`ObjectDisposedException`になります
