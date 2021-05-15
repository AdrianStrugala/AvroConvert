using BenchmarkDotNet.Running;

namespace GrandeBenchmark
{
    public class Program
    {
        public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}