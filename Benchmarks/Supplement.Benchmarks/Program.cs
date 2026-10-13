using BenchmarkDotNet.Running;

namespace Supplement.Benchmarks
{
    public static class Program
    {
        // 引数はBenchmarkDotNetにそのまま渡す。例: dotnet run -c Release -- --filter *Publish*
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
