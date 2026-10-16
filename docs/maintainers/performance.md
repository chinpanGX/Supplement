# パフォーマンスとGC(メンテナー向け)

Supplementは、毎フレームや操作のたびに呼ばれる処理でGCアロケーションを起こさないことを方針にしています。使えるC#は9まで(Unity 6)なので、`ref`フィールドや補間文字列ハンドラーは使えません。

## 実装の方針

- **一時バッファは`ArrayPool`から借ります。** サイズで`stackalloc`と使い分けることはしません
- **一時リストは、Unityに依存しないコードでは[`ScopedList`・`RentedList`](../features/collections.md)を使います。** Unityに依存するコードで`List<T>`のAPIが必要なら`ListPool<T>`で構いません
- **列挙子は構造体で返します。** `foreach`の対象になる型は、`GetEnumerator()`で構造体の列挙子を返し、インターフェースの`GetEnumerator`は明示的に実装します(`EquatableReadOnlyList`・`RentedList`)。`async`メソッドの中の`foreach`で使う型は、列挙子を`ref struct`にしません
- **同期的に完了しうる処理を、`async`メソッドで包みません。** エディタ(Code OptimizationがDebug)では、`async`メソッドのステートマシンが構造体ではなくクラスになり、同期的に完了しても呼ぶたびに確保されます。`GetAwaiter().IsCompleted`で完了を確かめ、終わっていないときだけ`async`メソッドに渡します(`MessageBroker`の非同期の購読者)
- **LINQは使いません**(ホットパスでは)。列挙子とデリゲートを確保するためです。添字のループにします
- **頻繁に作る`IDisposable`は構造体にします**(`TapGuard.GuardHandle`)。`using`で受ければボックス化しません
- **発行のたびにコピーしません。** 購読者自身をノードにした双方向リストにし、発行は開始時の版でノードを絞ります(`MessageBroker`。仕組みは[内部の設計](internals.md))
- **型ごとの値は、ジェネリックの静的クラスにキャッシュします**(`EnumCache<T>`、`MessageBroker`の型ごとの連番)
- **MonoBehaviourの`Update()`を増やしません。** 毎フレームの処理は`UpdateDispatcher`にまとめます

GCアロケーションを起こさないことは、テストで確かめます。書き方は[テスト](testing.md)を参照してください。
