using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.Base;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public static class SerialPingDependencyInjection
{
    public static IServiceCollection RegisterSerialPingCommand(this IServiceCollection services)
    {
        services.AddSingleton<ISerialPortEnumerator, DefaultSerialPortEnumerator>();
        services.AddTransient<IPingDataCollector, InMemoryPingDataCollector>();

        return services;
    }
}

public class SerialPingSettings(ISerialPortEnumerator portEnumerator) : BaseSerialPingSettings(portEnumerator)
{
    
}



public class SerialPingCommand(IAnsiConsole console, IPingDataCollector dataCollector, ILogger<SerialPingCommand> logger) : CancellableAsyncCommand<SerialPingSettings>
{
    private readonly ILogger<SerialPingCommand> _logger = logger;

    PingSendPongReceiveService _pingSendPongReceiveService = new PingSendPongReceiveService(dataCollector);
    public override async Task<int> ExecuteAsync(CommandContext context, SerialPingSettings settings, CancellationToken cancellationToken)
    {
        console.MarkupLine("[yellow]Press Ctrl+C to stop[/]");
        using var serialPort = settings.OpenPortWithVisualSummary(console);

        await DrawAsync(serialPort.BaseStream, cancellationToken);

        return 0;
    }

    async Task DrawAsync(Stream serialPortBaseStream, CancellationToken cancellationToken)
    {
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("StatsColumn").Ratio(1),
                new Layout("LogsColumn").Ratio(2)
            );

        await console.Live(layout)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                try
                {
                    layout["StatsColumn"].Update(new Panel(new Text("Waiting for ping data...")).Header("Statistics"));
                    layout["LogsColumn"].Update(new Panel(new Text("Waiting for logs...")).Header("Detailed Logs"));

                    ctx.Refresh();

                    do
                    {
                        // start processing.
                        var result = await MyProcessAsync(serialPortBaseStream, cancellationToken);
                        DrawSummary(layout["StatsColumn"], result);
                        DrawLogs(layout["LogsColumn"], result);
                        ctx.Refresh();
                        try
                        {
                            await Task.Delay(250, cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogWarning("PingConsoleUI: Loop cancelled via Task.Delay exception.");
                        }
                    } while (!cancellationToken.IsCancellationRequested);
                }
                finally
                {
                    ctx.Refresh();
                }
            });
        ;

    }

    private void DrawSummary(Layout layout, IPingDataCollector result)
    {
        var summary = result.GetSummary();

        var statsTable = new Table()
            .Expand();

        statsTable.BorderStyle(new());
        statsTable.AddColumn("Metric");
        statsTable.AddColumn("Value");
        statsTable.AddRow("Total Pings", summary.TotalPings.ToString());
        statsTable.AddRow("Successful", summary.SuccessfulPings.ToString());
        statsTable.AddRow("Failed", summary.FailedPings.ToString());
        statsTable.AddRow("Avg Latency (ms)", summary.AverageLatencyMs.ToString("F2"));

        var rows = new Rows(statsTable, new Markup("[yellow]Press Ctrl+C to stop[/]"));

        layout
            .Update(new Panel(rows)
                .Header("Statistics")
                .Expand());
    }

    private void DrawLogs(Layout layout, IPingDataCollector result)
    {
        var logEntries = result.GetRecentLogEntries(15); // Display last 15 logs

        var logRenderables = logEntries.Select(log =>
        {
            var color = log.Level switch
            {
                LogLevel.Error => "red",
                LogLevel.Warning => "yellow",
                LogLevel.Info => "blue", // Or another distinct color
                _ => "grey" // For LogLevel.Detail
            };

            return new Markup($"[{color}]{log.Timestamp:HH:mm:ss.fff} {log.Message.EscapeMarkup()}[/]");
        }).ToList<IRenderable>();

        var logsPanelContent = new Rows(logRenderables);
        layout
            .Update(new Panel(logsPanelContent)
                .Header("Detailed Logs")
                .Expand());
    }

    async Task<IPingDataCollector> MyProcessAsync(Stream serialPortBaseStream, CancellationToken cancellationToken)
    {
        await _pingSendPongReceiveService.SendPackageAndWaitForResponeAsync(serialPortBaseStream, cancellationToken);
        return dataCollector;
    }
}

// Supporting records/enums (can be in a shared location or with IPingDataCollector)
public record LogEntry(DateTime Timestamp, string Message, LogLevel Level, string? Source = null);
public enum LogLevel { Detail, Info, Warning, Error }
public record PingSummary
{
    public long TotalPings { get; set; }
    public long SuccessfulPings { get; set; }
    public long FailedPings { get; set; }
    public double TotalLatencyMs { get; set; }
    public double AverageLatencyMs => SuccessfulPings > 0 ? TotalLatencyMs / SuccessfulPings : 0;
    public List<PingResult> LastNPingResults { get; } = new();
    public int MaxLastNResults { get; set; } = 10; // Configurable
    // Add other relevant aggregate statistics like min/max latency if needed
}

// Forward declaration for PingResult, assuming it exists or will be created elsewhere.
// If PingResult is defined in another file within the same namespace, this might not be strictly necessary
// but helps clarify dependency if it's in a different namespace or assembly.
// For now, let's assume PingResult will be accessible.
// If it's defined in this project, ensure its namespace is imported or it's in the same namespace.
// public record PingResult( ... ); // Actual definition of PingResult is needed

public interface IPingDataCollector
{
    void AddLogEntry(LogEntry entry);
    IEnumerable<LogEntry> GetRecentLogEntries(int count); // For log panel
    void AddPingResult(PingResult result); // For statistics
    PingSummary GetSummary(); // For statistics table
}

public class PingResult
{
    public PingResultStatus Status { get; }
    public double LatencyMs { get; } // Only valid if Status is Success
    public int BytesReceived { get; } // Number of bytes received for successful pings
    public string? ErrorMessage { get; } // For IoError or potentially Corrupted

    private PingResult(PingResultStatus status, double latencyMs = 0, int bytesReceived = 0, string? errorMessage = null)
    {
        Status = status;
        LatencyMs = latencyMs;
        BytesReceived = bytesReceived;
        ErrorMessage = errorMessage;
    }

    public static PingResult SuccessResult(double latencyMs, int bytesReceived)
    {
        return new(PingResultStatus.Success, latencyMs, bytesReceived);
    }

    public static PingResult TimeoutResult()
    {
        return new(PingResultStatus.Timeout);
    }

    public static PingResult CorruptedResult(string? details = null)
    {
        return new(PingResultStatus.Corrupted, errorMessage: details);
    }

    public static PingResult IoErrorResult(string message)
    {
        return new(PingResultStatus.IoError, errorMessage: message);
    }

    public static PingResult CancelledResult()
    {
        return new(PingResultStatus.Cancelled);
        // Added factory method
    }
}

public enum PingResultStatus
{
    Success,
    Timeout,
    Corrupted,
    IoError,
    Cancelled // Added for explicit cancellation handling within the executor
}

public class InMemoryPingDataCollector : IPingDataCollector
{
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly List<PingResult> _allPingResults = new();
    private const int MaxLogEntries = 200; // Keep a rolling buffer of logs

    public void AddLogEntry(LogEntry entry)
    {
        _logEntries.Enqueue(entry);
        while (_logEntries.Count > MaxLogEntries && _logEntries.TryDequeue(out _)) { }
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
        var successfulPings = _allPingResults.Count(r => r.Status == PingResultStatus.Success);
        var summary = new PingSummary
        {
            TotalPings = _allPingResults.Count,
            SuccessfulPings = successfulPings,
            FailedPings = _allPingResults.Count(r => r.Status != PingResultStatus.Success),
            TotalLatencyMs = _allPingResults.Where(r => r.Status == PingResultStatus.Success).Sum(r => r.LatencyMs)
            // MaxLastNResults is already set in PingSummary record definition
        };
        summary.LastNPingResults.AddRange(_allPingResults.TakeLast(summary.MaxLastNResults).ToList());
        return summary;
    }
}
