using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

public static class StreamExtensions
{
    public static async Task<string?> ReadLineWithTimeoutAsync(this Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var fromSeconds = TimeSpan.FromSeconds(10);
   
        var readLineTask = ReadTask();
        var timeoutTask = Task.Delay(fromSeconds, timeoutCts.Token);
        var firstTask = await Task.WhenAny(readLineTask, timeoutTask);
        if (firstTask == readLineTask)
        {
            var readLine = await readLineTask;
            return readLine;
        }
        else
        {
            throw new TimeoutException($"Waiting for the incoming line took over {fromSeconds}.");
        }

        async Task<string> ReadTask()
        {
            string? read = null;
            while (read is null)
            {
                read = await reader.ReadLineAsync(timeoutCts.Token).AsTask();
            }

            return read;
        }
    }
}
