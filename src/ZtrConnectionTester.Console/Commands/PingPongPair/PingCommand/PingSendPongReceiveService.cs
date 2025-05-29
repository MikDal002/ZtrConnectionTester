using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

interface IPingSendPongReceiveService
{

}

public class PingSendPongReceiveService(IPingDataCollector pingPongDataCollector) : IPingSendPongReceiveService
{
    public async Task SendPackageAsync(Stream bidirectionalStream)
    {
        if (bidirectionalStream == null)
        {
            throw new ArgumentNullException(nameof(bidirectionalStream));
        }

        // Prepare the payload as a string and append a newline character
        const string payloadString = "PING\n";
        var payloadBytes = Encoding.ASCII.GetBytes(payloadString);

        try
        {
            // Write the payload to the stream
            await bidirectionalStream.WriteAsync(payloadBytes, 0, payloadBytes.Length);
            await bidirectionalStream.FlushAsync();
        }
        catch (Exception)
        {
            // Rethrow any exceptions for now without additional handling
            throw;
        }
    }

    public async Task SendPackageAndWaitForResponeAsync(Stream bidirectionalStream, CancellationToken cancellationToken)
    {
        try
        {
            // Send the package
            await SendPackageAsync(bidirectionalStream);

            // Read the response
            var response = await bidirectionalStream.ReadLineWithTimeoutAsync(cancellationToken);

            // Check if the response contains "PONG"
            if (response.Contains("PONG", StringComparison.OrdinalIgnoreCase))
            {
                pingPongDataCollector.AddLogEntry(new LogEntry(DateTime.UtcNow, "Received valid response: PONG",
                    LogLevel.Info));
            }
            else
            {
                pingPongDataCollector.AddLogEntry(new LogEntry(DateTime.UtcNow,
                    $"Unexpected response: `{response}`",
                    LogLevel.Warning));
            }
        }
        catch (TimeoutException)
        {
            pingPongDataCollector.AddLogEntry(new LogEntry(DateTime.Now, "Timeout occured!", LogLevel.Error));
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
