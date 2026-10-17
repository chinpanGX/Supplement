namespace Supplement.Benchmarks
{
    public struct Message
    {
        public int Value;
    }

    // ハンドラーの中身が最適化で消えないよう、受け取った値を足し込む先。
    public static class Sink
    {
        public static int Sum;
    }
}
