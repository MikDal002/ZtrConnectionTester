using Spectre.Console;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;

public interface IPongReturnService
{
    Task StartAsync(Stream stream, CancellationToken cancellationToken);
}
public class PongReturnService(IAnsiConsole console) : IPongReturnService
{
    public async Task StartAsync(Stream stream, CancellationToken cancellationToken)
    {
        do
        {
            await ReceivePackageAndReturnItAsync(stream, cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }

    public async Task ReceivePackageAndReturnItAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var incomingMessage = await stream.ReadLineWithTimeoutAsync(timeout: TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            if (incomingMessage.Contains("PING", StringComparison.OrdinalIgnoreCase))
            {
                using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
                await writer.WriteAsync("PONG\n");
                await writer.FlushAsync(cancellationToken);
                console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] PONG sent back!");
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
    }
}
