using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.NETCore.Client;
using System.Diagnostics;

namespace BenchmarkDotNet.Issue2779;

public class Benchmarks
{
    private void GcDump(string key)
    {
        int pid = Environment.ProcessId;
        var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

        if (workspace == null)
        {
            workspace = Path.Combine(Path.GetTempPath(), "BenchmarkDotNet_GcDump");
            Directory.CreateDirectory($"{workspace}/artifacts");
        }

        string outputPath = $"{workspace}/artifacts/{DateTime.Now:yyyyMMdd_HHmmss_fff}_{key}.gcdump";

        var process = Process.Start(new ProcessStartInfo("dotnet", $"gcdump collect -p {pid} --output {outputPath}")
        {
            CreateNoWindow = false,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        })!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Console.WriteLine(stdout);
    }

    // Comment out because it might affects results.
    ////[IterationSetup]
    ////public void IterationSetup()
    ////{
    ////    GcDump("warmup");
    ////    GcDump("setup");
    ////}

    ////[IterationCleanup]
    ////public void IterationCleanup()
    ////{
    ////    GcDump("cleanup");
    ////}

    [Benchmark]
    public byte[] SixtyFourBytesArray()
    {
        // this benchmark should hit allocation quantum problem
        // it allocates a little of memory, but it takes a lot of time to execute so we can't run in thousands of times!

        Thread.Sleep(TimeSpan.FromSeconds(0.5));

        return new byte[64];
    }
}
