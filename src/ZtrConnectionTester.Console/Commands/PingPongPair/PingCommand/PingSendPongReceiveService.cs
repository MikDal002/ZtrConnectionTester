using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

interface IPingSendPongReceiveService
{
    Task SendPackageAndWaitForResponseAsync(Stream bidirectionalStream, CancellationToken cancellationToken);
}

public class PingSendPongReceiveService(IPingDataCollector pingPongDataCollector) : IPingSendPongReceiveService
{
    public static async Task SendPackageAsync(Stream bidirectionalStream, CancellationToken cancellationToken)
    {
        if (bidirectionalStream == null)
        {
            throw new ArgumentNullException(nameof(bidirectionalStream));
        }

        // Prepare the payload as a string and append a newline character
        const string payloadString = "PING\n";
        var payloadBytes = Encoding.ASCII.GetBytes(payloadString);

        // Write the payload to the stream
        await bidirectionalStream.WriteAsync(payloadBytes, 0, payloadBytes.Length, cancellationToken);
        await bidirectionalStream.FlushAsync(cancellationToken);
    }

    public async Task SendPackageAndWaitForResponseAsync(Stream bidirectionalStream, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Send the package
            await SendPackageAsync(bidirectionalStream, cancellationToken);

            // Read the response
            var response = await bidirectionalStream.ReadLineWithTimeoutAsync(timeout: TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            stopwatch.Stop();

            // Check if the response contains "PONG"
            if (response.Contains("PONG", StringComparison.OrdinalIgnoreCase))
            {
                pingPongDataCollector.AddPingResult(PingResult.SuccessResult(stopwatch.Elapsed, response.Length));
            }
            else
            {
                pingPongDataCollector.AddPingResult(PingResult.CorruptedResult($"Unexpected response: `{response}`"));
            }
        }
        catch (TimeoutException)
        {
            pingPongDataCollector.AddPingResult(PingResult.TimeoutResult());
        }
        catch (TaskCanceledException)
        {
            // Nothing interesting
        }
        catch (Exception ex)
        {
            // Log the exception
            pingPongDataCollector.AddLogEntry(new LogEntry(DateTime.UtcNow, $"Error: {ex.Message}",
                LogLevel.Error));
            throw;
        }
    }
}
