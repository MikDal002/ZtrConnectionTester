using Spectre.Console.Testing;
using System.Text;
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
        _pongService = new(new TestConsole());
        _pingPongDataCollector = new InMemoryPingDataCollector();
        _pingService = new(_pingPongDataCollector);
    }

    private PongReturnService _pongService = null!;
    private PingSendPongReceiveService _pingService = null!;
    private IPingDataCollector _pingPongDataCollector = null!;

    [Test]
    [CancelAfter(2000)]
    public async Task PingPong_SuccessfulCycle_WithSendPackageAndWaitForResponseAsync()
    {
        // Arrange
        var streamCrossover = new StreamCrossover();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var pingTask = Task.Run(() => _pingService.SendPackageAndWaitForResponseAsync(streamCrossover.GetFor1Device(), cancellationToken), cancellationToken);
        var pongTask = Task.Run(() => _pongService.StartAsync(streamCrossover.GetFor2Device(), cancellationToken), cancellationToken);

        // Act
        await Task.WhenAll(pingTask, pongTask);

        // Assert
        var logEntries = _pingPongDataCollector.GetRecentLogEntries(10);
        logEntries.Should().NotBeEmpty("because the ping-pong cycle should generate log entries");
        logEntries.Should().Contain(entry => entry.Message.Contains("Success"), "because a response should be received");

    }

    [Test]
    [CancelAfter(2000)]
    public async Task PingPong_SuccessfulCycle_ShouldReturnSuccessAndLogEvents()
    {
        // Arrange

        // Create a custom stream for testing
        var streamCrossover = new StreamCrossover();

        var sendTask = Task.Run(async () => await PingSendPongReceiveService.SendPackageAsync(streamCrossover.GetFor1Device()));
        var receiveTask = Task.Run(async () => await _pongService.StartAsync(streamCrossover.GetFor2Device(), CancellationToken.None));

        await Task.WhenAll(sendTask, receiveTask);

        using var reader = new StreamReader(streamCrossover.GetFor1Device(), Encoding.ASCII, leaveOpen: true);

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

public class StreamCrossover
{
    readonly Stream _first = new MemoryStream();
    readonly Stream _second = new MemoryStream();

    public CrossedStream GetFor1Device()
    {
        return new(_first, _second);
    }

    public CrossedStream GetFor2Device()
    {
        return new(_second, _first);
    }
}

public class CrossedStream : Stream
{
    readonly Stream _readStream;
    readonly Stream _writeStream;

    public CrossedStream(Stream readStream, Stream writeStream)
    {
        _readStream = readStream;
        _writeStream = writeStream;
    }

    public override void Flush()
    {
        _readStream.Flush();
        _writeStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _readStream.Read(buffer, offset, count);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException("Seeking is not supported in ConsumableStream.");

    public override void SetLength(long value)
        => throw new NotSupportedException("Setting length is not supported in ConsumableStream.");

    public override void Write(byte[] buffer, int offset, int count)
    {
        _writeStream.Write(buffer, offset, count);
        _writeStream.Position -= count;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        _writeStream.Position -= count;
    }

    public override bool CanRead => _readStream.CanRead;
    public override bool CanSeek => false; // Seeking is not supported
    public override bool CanWrite => _writeStream.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}

//public class ConsumableStream : Stream
//{
//    private readonly MemoryStream _innerStream;
//    public ConsumableStream()
//    {
//        _innerStream = new MemoryStream();
//    }
//    public ConsumableStream(byte[] initialData)
//    {
//        _innerStream = new MemoryStream(initialData);
//    }

//    public override bool CanRead => _innerStream.CanRead;
//    public override bool CanSeek => false; // Seeking is not supported
//    public override bool CanWrite => _innerStream.CanWrite;
//    public override long Length => throw new NotSupportedException();
//    public override long Position
//    {
//        get => throw new NotSupportedException();
//        set => throw new NotSupportedException();
//    }
//    public override void Flush()
//    {
//        _innerStream.Flush();
//    }
//    public override async Task FlushAsync(CancellationToken cancellationToken)
//    {
//        await _innerStream.FlushAsync(cancellationToken);
//    }
//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        int bytesRead = _innerStream.Read(buffer, offset, count);
//        // Remove the read data from the stream
//        //RemoveReadData(bytesRead);
//        return bytesRead;
//    }
//    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
//    {
//        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
//        // Remove the read data from the stream
//        //RemoveReadData(bytesRead);
//        return bytesRead;
//    }
//    public override void Write(byte[] buffer, int offset, int count)
//    {
//        _innerStream.Write(buffer, offset, count);
//        _innerStream.Position -= count;
//    }
//    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
//    {
//        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
//        _innerStream.Position -= count;
//    }
//    public override long Seek(long offset, SeekOrigin origin)
//    {
//        throw new NotSupportedException("Seeking is not supported in ConsumableStream.");
//    }
//    public override void SetLength(long value)
//    {
//        throw new NotSupportedException("Setting length is not supported in ConsumableStream.");
//    }
//    private void RemoveReadData(int bytesRead)
//    {
//        if (bytesRead > 0)
//        {
//            // Create a new buffer with the remaining data
//            byte[] remainingData = new byte[_innerStream.Length - _innerStream.Position];
//            _innerStream.Read(remainingData, 0, remainingData.Length);
//            // Reset the inner stream and write back the remaining data
//            _innerStream.SetLength(0);
//            _innerStream.Write(remainingData, 0, remainingData.Length);
//            _innerStream.Position = 0;
//        }
//    }
//    protected override void Dispose(bool disposing)
//    {
//        if (disposing)
//        {
//            _innerStream.Dispose();
//        }
//        base.Dispose(disposing);
//    }
//}
