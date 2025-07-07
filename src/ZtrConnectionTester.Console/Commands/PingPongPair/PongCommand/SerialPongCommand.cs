using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.Base;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;

public class SerialPongCommand(
    IAnsiConsole console,
    IPongReturnService pongReturnService,
    ILogger<SerialPongCommand> logger) : CancellableAsyncCommand<SerialPongSettings>
{
    private readonly ILogger<SerialPongCommand> _logger = logger;

    public override async Task<int> ExecuteAsync(CommandContext context, SerialPongSettings settings,
        CancellationToken cancellationToken)
    {
        using var serialPort = settings.OpenPortWithVisualSummary(console);

        console.MarkupLine("[yellow]Press Ctrl+C to stop[/]");
        await pongReturnService.StartAsync(serialPort.BaseStream, cancellationToken);

        return 0;
    }
}
