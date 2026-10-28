using System.Diagnostics;
using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

// Runs alone, so the timing doesn't include other tests using the same server.
[CollectionDefinition(nameof(TimingTests), DisableParallelization = true)]
public class TimingCollection;

// Q5: the database checks for 4 contexts of about 20 migrations each take under a minute on a warm server.
[Collection(nameof(TimingTests))]
public class TimingTests
{
    [Fact]
    public async Task RunAsync_ShouldCheckFourContextsInUnderAMinute_WhenServerIsWarm()
    {
        var server = SqlServer.ConnectionString();
        await using (var warmUp = ToyContexts.Create("Checklist"))
        {
            await ParityCheck.RunAsync(warmUp, ScriptGenerator.Generate(warmUp), server);
        }

        var report = new List<string>();
        var total = Stopwatch.StartNew();
        foreach (var name in ToyContexts.Names)
        {
            var one = Stopwatch.StartNew();
            await using var context = ToyContexts.Create(name);
            var problems = await ParityCheck.RunAsync(context, SqlServer.ScriptFor(context), server);
            Assert.Empty(problems);
            report.Add($"{name}: {one.Elapsed.TotalSeconds:F1} s");
        }

        report.Add($"Total: {total.Elapsed.TotalSeconds:F1} s");
        Report(report);
        Assert.True(total.Elapsed < TimeSpan.FromSeconds(60), string.Join(", ", report));
    }

    // The CI job summary shows the timings without digging through the log.
    private static void Report(List<string> lines)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(Environment.NewLine, lines));
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summary)
        {
            File.AppendAllLines(summary, ["### Database tier timing", "", .. lines.Select(line => "- " + line)]);
        }
    }
}
