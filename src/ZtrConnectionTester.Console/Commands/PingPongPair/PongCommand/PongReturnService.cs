using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.Base;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;

public interface IPongReturnService
{
    Task ReceivePackageAndReturnItAsync(Stream stream, CancellationToken cancellationToken);
}
public class PongReturnService(IAnsiConsole console) : IPongReturnService
{
    public async Task ReceivePackageAndReturnItAsync(Stream stream, CancellationToken cancellationToken)
    {
        do
        {
            try
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                var incomingMessage = await stream.ReadLineWithTimeoutAsync(cancellationToken);

                if (incomingMessage.Contains("PING", StringComparison.OrdinalIgnoreCase))
                {
                    using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
                    await writer.WriteAsync("PONG\n");
                    await writer.FlushAsync(cancellationToken);
                    console.WriteLine("PONG sent back!");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Incoming message does not contain PING. Contains {incomingMessage}.");
                }
            }
            catch (TimeoutException ex)
            {
                console.WriteLine(ex.Message);
            }
            catch (TaskCanceledException)
            {
                // Nothing interesting
            }
        } while (!cancellationToken.IsCancellationRequested);
    }
}


public static class SerialPongDependencyInjection
{
    public static IServiceCollection RegisterSerialPongCommand(this IServiceCollection services)
    {
        services.AddSingleton<ISerialPortEnumerator, DefaultSerialPortEnumerator>();
        services.AddTransient<IPongReturnService, PongReturnService>();

        return services;
    }
}

public class SerialPongSettings(ISerialPortEnumerator portEnumerator) : BaseSerialPingSettings(portEnumerator)
{
}



public class SerialPongCommand(
    IAnsiConsole console,
    IPongReturnService pongReturnService,
    ILogger<SerialPongCommand> logger) : CancellableAsyncCommand<SerialPongSettings>
{
    private readonly ILogger<SerialPongCommand> _logger = logger;


    public override async Task<int> ExecuteAsync(CommandContext context, SerialPongSettings settings,
        CancellationToken cancellationToken)
    {
        console.MarkupLine("[yellow]Press Ctrl+C to stop[/]");
        using var serialPort = settings.OpenPortWithVisualSummary(console);

        do
        {
            await pongReturnService.ReceivePackageAndReturnItAsync(serialPort.BaseStream, cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);

        return 0;
    }
}
