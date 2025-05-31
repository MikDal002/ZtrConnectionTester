using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

public static class StreamExtensions
{
    public static async Task<string> ReadLineWithTimeoutAsync(this Stream stream, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var effectiveTimeout = timeout == TimeSpan.Zero ? TimeSpan.FromSeconds(1) : timeout;

        timeoutCts.CancelAfter(effectiveTimeout);

        try
        {
            return await ReadTask();
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == timeoutCts.Token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            else
            {
                throw new TimeoutException($"Waiting for the incoming line took over {effectiveTimeout}.", oce);
            }
        }
        // Other exceptions (including OperationCanceledException from a different token) will propagate.

        async Task<string> ReadTask()
        {
            string? read = null;
            while (read is null)
            {
                read = await reader.ReadLineAsync(timeoutCts.Token);
            }

            return read;
        }
    }
}
