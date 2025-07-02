using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkSwitcher.FromTypes(new[] {
            typeof(FileGenerationBenchmark),
            typeof(SortFileBenchmark)
        }).Run(args);
    }
}
