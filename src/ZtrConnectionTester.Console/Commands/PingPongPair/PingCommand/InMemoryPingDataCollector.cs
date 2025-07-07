using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public interface IPingDataCollector
{
    void AddLogEntry(LogEntry entry);
    IEnumerable<LogEntry> GetRecentLogEntries(int count); // For log panel
    void AddPingResult(PingResult result); // For statistics
    PingSummary GetSummary(); // For statistics table
}

public class InMemoryPingDataCollector : IPingDataCollector
{
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly List<PingResult> _allPingResults = new();
    private const int MaxLogEntries = 200; // Keep a rolling buffer of logs

    public void AddLogEntry(LogEntry entry)
    {
        _logEntries.Enqueue(entry);
        while (_logEntries.Count > MaxLogEntries && _logEntries.TryDequeue(out _))
        {
        }
    }

    public IEnumerable<LogEntry> GetRecentLogEntries(int count) =>
        _logEntries.Reverse().Take(count).Reverse().ToList(); // Show newest at bottom

    public void AddPingResult(PingResult result)
    {
        _allPingResults.Add(result);

        AddLogEntry(new LogEntry(DateTime.UtcNow, $"Ping {result.Status}, Latency: {result.LatencyMs:F2}ms, Err: {result.ErrorMessage ?? "N/A"}", LogLevel.Detail, "PingResult"));
    }

    public PingSummary GetSummary()
    {
        var successfulPingsResults = _allPingResults.Where(r => r.Status == PingResultStatus.Success).ToList();
        var successfulPings = successfulPingsResults.Count;
        var latencies = successfulPingsResults.Select(r => r.LatencyMs).ToList();

        var summary = new PingSummary
        {
            TotalPings = _allPingResults.Count,
            SuccessfulPings = successfulPings,
            FailedPings = _allPingResults.Count(r => r.Status != PingResultStatus.Success),
            AverageLatencyMs = successfulPings > 0 ? latencies.Average() : 0,
            StandardDeviationLatencyMs = CalculateStandardDeviation(latencies),
            Percentile99LatencyMs = CalculatePercentile(latencies, 0.99),
            LastNPingResults = _allPingResults/*.TakeLast(MaxLogEntries)*/.ToList() // Default to 10, maybe this should be configurable
        };
        
        return summary;
    }

    private static double CalculateStandardDeviation(IReadOnlyCollection<double> values)
    {
        if (values.Count < 2)
            return 0;

        var avg = values.Average();
        var sumOfSquares = values.Sum(val => (val - avg) * (val - avg));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    private static double CalculatePercentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        var n = sortedValues.Count;
        var rank = percentile * (n - 1);
        var integralRank = (int)Math.Floor(rank);
        var fractionalRank = rank - integralRank;

        if (integralRank >= n - 1)
            return sortedValues[n - 1];

        var lowerValue = sortedValues[integralRank];
        var upperValue = sortedValues[integralRank + 1];

        return lowerValue + fractionalRank * (upperValue - lowerValue);
    }
}
