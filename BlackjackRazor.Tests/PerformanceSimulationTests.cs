using Xunit;
using BlackjackRazor.Simulation;

namespace BlackjackRazor.Tests;

public class PerformanceSimulationTests
{
    [Fact(Skip="Perf harness manual run – enable locally when needed.")]
    public void Run_2000_Rounds_Fast()
    {
        var result = PerformanceSimulator.Run(rounds: 2000, startingBankroll: 1000m, bet: 10m);
        // Basic sanity assertions – should complete all rounds, final bankroll non-negative.
        Assert.Equal(2000, result.Rounds);
        Assert.True(result.FinalBankroll > 0m);
        // Log to console for manual inspection.
        System.Console.WriteLine(result);
    }
}
