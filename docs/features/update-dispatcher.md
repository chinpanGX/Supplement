# Updateの集約(UpdateDispatcher)

アセンブリ・名前空間: `Supplement.Unity`

MonoBehaviourのUpdateを、PlayerLoopに差し込んだ1か所からまとめて呼びます。

MonoBehaviourの`Update()`は、インスタンスごとにネイティブからマネージドへの呼び出しが起きます。毎フレーム動くコンポーネントが多いと、その分が重くなります。`UpdateDispatcher`に登録すると、呼び出しは1回のループにまとまります。

## 使い方

```csharp
public sealed class Spinner : MonoBehaviour, IUpdatable
{
    private void OnEnable() => UpdateDispatcher.Register(this);
    private void OnDisable() => UpdateDispatcher.Unregister(this);

    public void OnUpdate()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
    }
}
```

## 動き

- PlayerLoopの`Update`の最後に差し込みます(MonoBehaviourの`Update()`と同じフェーズ)
- 呼び出し順は登録順とは限りません
- 同じものを2回登録しても、呼ぶのは1回です。登録していないものを解除しても何もしません
- `OnUpdate`の中で登録したものは、次のフレームから呼びます。`OnUpdate`の中で解除しても、ほかの要素は飛ばされません
- `OnUpdate`が例外を投げてもログに出すだけで、ほかの要素は呼び続けます
- 解除し忘れたまま破棄されたMonoBehaviourは、自動で外します
- 登録・解除はどちらも定数時間です
- ドメインリロードを無効にしていても、プレイを始めるたびに登録とPlayerLoopへの差し込みを作り直します

## 使い分け

- **MonoBehaviour**: `UpdateDispatcher`
- **DIコンテナに登録するピュアC#のクラス**: VContainerの`ITickable`(こちらも1か所から呼ばれます)

