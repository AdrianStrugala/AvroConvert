using BenchmarkDotNet.Running;

namespace SolTechnology.PerformanceBenchmark
{
    public class Program
    {
        public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}