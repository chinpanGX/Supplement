# 戻るボタン(BackKeyReceiver)

アセンブリ・名前空間: `Supplement.Unity`

`Button`と同じGameObjectに`BackKeyReceiver`を付けると、Androidの戻るボタン(エディタやPCではEscキー)で、そのButtonのクリックを発火させます。画面を閉じるボタンや、モーダルの「キャンセル」に付けます。

## 発火する条件

- そのButtonが`interactable`である
- そのButtonが、画面上でRaycast的に最前面にある。中心と4隅の5点で判定し、1点でも当たれば最前面とみなします(判定点は`insetPixels`だけ内側にずらす。Rectの幅・高さの半分が上限)
- シーンに`EventSystem`がある(無ければ最前面とみなさない)

画面遷移のスタックなどの知識は持たず、「見えていて押せるか」だけで判定します。上にモーダルが重なっていれば、下の画面のボタンは発火しません。

## 入力

- Input Systemのパッケージが入っていて有効なら、Input Systemから読みます(Androidの戻るボタンはKeyboardの`escapeKey`として届きます)
- そうでなければ旧Input Managerの`Input.GetKeyDown(KeyCode.Escape)`を使います
- どちらも有効でなければ、戻るボタンを読みません(発火しない)

戻るボタンの入力は、Receiverの数にかかわらず1フレームに1回だけ読みます。押されたフレームだけ、各Receiverの最前面判定をします。
上の画面のボタンで画面が閉じても、同じフレームで下の画面のボタンが続けて発火することはありません。

## DIへの登録

`BackKeyReceiver`を動かすだけなら、DIへの登録は要りません。

BackKey Event Viewer(下の「デバッグ」)を使う場合は、`IContainerBuilder`(VContainer)の拡張メソッド`RegisterBackKeyDebugRegistry()`で、Receiverの一覧を登録します。開発ビルドだけで登録するのがおすすめです。

```csharp
if (Debug.isDebugBuild)
{
    builder.RegisterBackKeyDebugRegistry();
}
```

`BackKeyReceiver`は、これが登録されていれば`[Inject]`で受け取ります(無ければ一覧に載らないだけです)。そのため、DIコンテナ経由で生成したときだけ一覧に出ます。

## デバッグ

- Receiverを選択すると、Scene Viewに判定点(中心+4隅)と、それを囲む枠を水色で表示します。枠はRectを`insetPixels`だけ内側に縮めたものです。エディタだけの表示で、ビルドには含まれません
- `Window > Supplement > BackKey Event Viewer`で、登録中のReceiverと、最前面にあるReceiver(ACTIVE)を確認できます(Play中のみ)。ACTIVEは最前面かどうかだけで、`interactable`は見ていません

## 注意

- Canvasの下に置く必要があります。無ければ、戻るボタンを押したときの判定で例外をログに出し、そのReceiverは発火しません(ほかのReceiverは判定を続けます)
- 一度Canvasの下に置いたら、別のCanvasへ付け替えない前提でCanvasをキャッシュしています
