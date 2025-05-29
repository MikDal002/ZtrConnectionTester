using System.IO.Pipelines;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.Text;
using System.Threading;
using ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;
using ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;
// ReSharper disable AccessToDisposedClosure

namespace ZtrConnectionTester.Console.Tests.Commands.SerialPing;

[TestFixture]
public class PingPongIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        _pongService = new();
        _pingPongDataCollector = new InMemoryPingDataCollector();
        _pingService = new(_pingPongDataCollector);
        _pingToPongPipe = new();
        _pongToPingPipe = new();
    }


    private PongReturnService _pongService = null!;
    private PingSendPongReceiveService _pingService = null!;
    private IPingDataCollector _pingPongDataCollector = null!;
    private Pipe _pingToPongPipe = null!;
    private Pipe _pongToPingPipe = null!;

    [Test]
    [Timeout(2000)]
    public async Task PingPong_SuccessfulCycle_WithSendPackageAndWaitForResponeAsync()
    {
        // Arrange
        var stream = new ConsumableStream();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var pongTask = Task.Run(() => _pongService.ReceivePackageAndReturnItAsync(stream, cancellationToken), cancellationToken);
        var pingTask = Task.Run(() => _pingService.SendPackageAndWaitForResponeAsync(stream, cancellationToken), cancellationToken);

        // Act
        await Task.WhenAll(pingTask, pongTask);

        // Assert
        var logEntries = _pingPongDataCollector.GetRecentLogEntries(10);
        logEntries.Should().NotBeEmpty("because the ping-pong cycle should generate log entries");
        logEntries.Should().Contain(entry => entry.Message.Contains("Received valid response: PONG"), "because a response should be received");

    }

    [Test]
    [Timeout(2000)]
    public async Task PingPong_SuccessfulCycle_ShouldReturnSuccessAndLogEvents()
    {
        // Arrange

        // Create a custom stream for testing
        using var testingStream = new ConsumableStream();
        using var writer = new StreamWriter(testingStream, Encoding.ASCII, leaveOpen: true);
        using var reader = new StreamReader(testingStream, Encoding.ASCII, leaveOpen: true);
        
        
        var sendTask = Task.Run(async () =>
        {
            await _pingService.SendPackageAsync(testingStream);
        });
        var receiveTask = Task.Run(async () =>
        {
            await _pongService.ReceivePackageAndReturnItAsync(testingStream, CancellationToken.None);
        });
        await Task.WhenAll(sendTask, receiveTask);
        
        
        // Validate the result
        var result = await reader.ReadToEndAsync();
        result.Should().Contain("PONG\n", "because the stream should contain the 'PONG' message");



        // Act
        //var pingTask = Task.Run(() =>)
        //var pongTask = Task.Run(() => _pongService.RunAsync(pongInputStream, pongOutputStream, pongCts.Token),
        //    pongCts.Token);

        //PingResult pingResult = null!;
        //try
        //{
        //    pingResult = await _pingService.ExecutePingCycleAsync(
        //        _pingStream,
        //        payloadSize,
        //        timeoutMs,
        //        pingCts.Token);
        //}
        //finally
        //{
        //    pongCts.Cancel();
        //    try { await Task.WhenAny(pongTask, Task.Delay(100)); }
        //    catch
        //    {
        //        /* Ignore */
        //    }
        //}

        //// Assert
        //pingResult.Should().NotBeNull();
        //pingResult.Status.Should().Be(PingResultStatus.Success, "because PongService should echo the packet correctly");

        //var consoleOutput = _testConsole.Output;

        //consoleOutput.Should().Contain("Timestamp", "because the header should contain 'Timestamp'");
        //consoleOutput.Should().Contain("Direction", "because the header should contain 'Direction'");
        //consoleOutput.Should().Contain("Bytes", "because the header should contain 'Bytes'");
        //consoleOutput.Should().Contain("Sent", "because the header should contain 'Sent'");

        //var expectedInPattern = $@"IN(.*)({expectedResponseSize})(.*)no";
        //consoleOutput.Should().MatchRegex(new Regex(expectedInPattern, RegexOptions.Multiline),
        //    "because the logger should log received bytes with essential data (direction, bytes, sent status)");

        //var expectedOutPattern = $@"OUT(.*)({expectedResponseSize})(.*)yes";
        //consoleOutput.Should().MatchRegex(new Regex(expectedOutPattern, RegexOptions.Multiline),
        //    "because the logger should log sent bytes with essential data (direction, bytes, sent status)");

        //consoleOutput.Should().Contain("Pong service started. Waiting for data...");
        //consoleOutput.Should().Contain("Pong service stopped.");
    }
}
public class ConsumableStream : Stream
{
    private readonly MemoryStream _innerStream;
    public ConsumableStream()
    {
        _innerStream = new MemoryStream();
    }
    public ConsumableStream(byte[] initialData)
    {
        _innerStream = new MemoryStream(initialData);
    }
    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => false; // Seeking is not supported
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override void Flush()
    {
        _innerStream.Flush();
    }
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _innerStream.FlushAsync(cancellationToken);
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = _innerStream.Read(buffer, offset, count);
        // Remove the read data from the stream
        //RemoveReadData(bytesRead);
        return bytesRead;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        // Remove the read data from the stream
        //RemoveReadData(bytesRead);
        return bytesRead;
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        _innerStream.Position -= count;
    }
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        _innerStream.Position -= count;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking is not supported in ConsumableStream.");
    }
    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting length is not supported in ConsumableStream.");
    }
    private void RemoveReadData(int bytesRead)
    {
        if (bytesRead > 0)
        {
            // Create a new buffer with the remaining data
            byte[] remainingData = new byte[_innerStream.Length - _innerStream.Position];
            _innerStream.Read(remainingData, 0, remainingData.Length);
            // Reset the inner stream and write back the remaining data
            _innerStream.SetLength(0);
            _innerStream.Write(remainingData, 0, remainingData.Length);
            _innerStream.Position = 0;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
