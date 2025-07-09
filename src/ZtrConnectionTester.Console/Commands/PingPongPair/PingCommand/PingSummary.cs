using System;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public record PingSummary
{
    public required long TotalPings { get; init; }
    public required long SuccessfulPings { get; init; }
    public required long FailedPings { get; init; }
    public required TimeSpan AverageLatency { get; init; }
    public required TimeSpan StandardDeviationLatency { get; init; }
    public required TimeSpan Percentile99Latency { get; init; }
    public required double DownloadThroughputBps { get; init; }
    public required TimeSpan MaxLatency { get; init; }
}
