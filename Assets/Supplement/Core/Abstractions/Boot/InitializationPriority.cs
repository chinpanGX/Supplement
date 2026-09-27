namespace Supplement.Core
{
    /// <summary>
    /// <see cref="IBootInitializationTask"/>の実行順。値が小さいものから順に実行される。
    /// </summary>
    public enum InitializationPriority
    {
        Highest = 0,
        High = 100,
        AboveNormal = 200,
        Normal = 300,
        BelowNormal = 400,
        Low = 500,
        Lowest = 600,
    }
}
