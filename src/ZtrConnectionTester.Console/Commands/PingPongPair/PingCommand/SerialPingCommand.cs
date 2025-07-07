using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.Base;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public class SerialPingCommand(IAnsiConsole console, IPingDataCollector dataCollector, ILogger<SerialPingCommand> logger) : CancellableAsyncCommand<SerialPingSettings>
{
    PingSendPongReceiveService _pingSendPongReceiveService = new(dataCollector);
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
                            logger.LogWarning("PingConsoleUI: Loop cancelled via Task.Delay exception.");
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

    private static void DrawSummary(Layout layout, IPingDataCollector result)
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
        statsTable.AddRow("Latency Std (ms)", summary.StandardDeviationLatencyMs.ToString("F2"));
        statsTable.AddRow("Slowest .99 (ms)", summary.Percentile99LatencyMs.ToString("F2"));

        var rows = new Rows(statsTable, new Markup("[yellow]Press Ctrl+C to stop[/]"));

        layout
            .Update(new Panel(rows)
                .Header("Statistics")
                .Expand());
    }

    private static void DrawLogs(Layout layout, IPingDataCollector result)
    {
        var logEntries = result.GetRecentLogEntries(70); // Display last 15 logs

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
        var panel = new Panel(logsPanelContent)
            .Header("Detailed Logs")
            .Expand();
        
        layout.Update(panel);
    }

    async Task<IPingDataCollector> MyProcessAsync(Stream serialPortBaseStream, CancellationToken cancellationToken)
    {
        await _pingSendPongReceiveService.SendPackageAndWaitForResponseAsync(serialPortBaseStream, cancellationToken);
        return dataCollector;
    }
}
