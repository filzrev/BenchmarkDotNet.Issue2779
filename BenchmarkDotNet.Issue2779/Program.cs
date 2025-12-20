using AwesomeAssertions;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Issue2779;

internal class Program
{
    static void Main(string[] args)
    {
        AllocationQuantumIsNotAnIssueForNetCore21Plus();
    }

    public static void AllocationQuantumIsNotAnIssueForNetCore21Plus()
    {
        // 1. 64 bytes for the array
        // 2. long objectAllocationOverhead = IntPtr.Size * 2; // 16 bytes (pointer to method table + object header word)
        // 3. long arraySizeOverhead = IntPtr.Size;            // 8 byte (array length)
        // Total: 88 bytes

        AssertAllocations(CsProjCoreToolchain.NetCoreApp10_0, warmupCount: 0, expectedAllocationBytes: 88);
    }

    private static void AssertAllocations(IToolchain toolchain, int warmupCount, int expectedAllocationBytes)
    {
        // Arrange
        var config = CreateConfig(toolchain, warmupCount);
        var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config);

        // Act
        var summary = BenchmarkRunner.Run(benchmarks);

        var benchmark = benchmarks.BenchmarksCases.First();
        var benchmarkReport = summary.Reports.First();
        var result = benchmarkReport.GcStats.GetBytesAllocatedPerOperation(benchmark);

        // Assert
        result.Should().Be(expectedAllocationBytes);
    }

    private static IConfig CreateConfig(
        IToolchain toolchain,
        int warmupCount)
    {
        var job = Job.ShortRun
            .WithEvaluateOverhead(false) // no need to run idle for this test
            .WithWarmupCount(warmupCount)
            .WithIterationCount(1) // Single iteration is enough for most of the tests.
            .WithGcForce(false)
            .WithGcServer(false)
            .WithGcConcurrent(false)
            // To prevent finalizers allocating out of our control, we hang the finalizer thread.
            // https://github.com/dotnet/runtime/issues/101536#issuecomment-2077647417
            .WithEnvironmentVariable("BENCHMARKDOTNET_UNITTEST_BLOCK_FINALIZER_FOR_MEMORYDIAGNOSER", "_ACTIVE")
            .WithToolchain(toolchain);

        return ManualConfig.CreateEmpty()
            // Tiered JIT can allocate some memory on a background thread, let's disable it by default to make our tests less flaky (#1542).
            // This was mostly fixed in net7.0, but tiered jit thread is not guaranteed to not allocate, so we disable it just in case.
            .AddJob(job.WithEnvironmentVariables(
                    new EnvironmentVariable("DOTNET_TieredCompilation", "0"),
                    new EnvironmentVariable("COMPlus_TieredCompilation", "0")
            ))
            .WithBuildTimeout(TimeSpan.FromSeconds(240))
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddLogger(ConsoleLogger.Default);
    }
}
