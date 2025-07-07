using System.Collections.Generic;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public record PingSummary
{
    public required long TotalPings { get; init; }
    public required long SuccessfulPings { get; init; }
    public required long FailedPings { get; init; }
    public required double AverageLatencyMs { get; init; }
    public required double StandardDeviationLatencyMs { get; init; }
    public required double Percentile99LatencyMs { get; init; }
    public required IReadOnlyList<PingResult> LastNPingResults { get; init; }
    public int MaxLastNResults { get; init; } = 10; // Configurable
}
