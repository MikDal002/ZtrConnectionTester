using Spectre.Console.Testing;
using System.Text;
using ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;
using ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;
using ZtrConnectionTester.Console.Tests.TestStream;

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
    public async Task PingPong_SuccessfulCycle_WithSendPackageAndWaitForResponseAsync(CancellationToken cancellationToken)
    {
        // Arrange
        var streamCrossover = new StreamCrossover();

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
    public async Task PingPong_SuccessfulCycle_ShouldReturnSuccessAndLogEvents(CancellationToken cancellationToken)
    {
        // Arrange

        // Create a custom stream for testing
        var streamCrossover = new StreamCrossover();

        var sendTask = Task.Run(async () => await PingSendPongReceiveService.SendPackageAsync(streamCrossover.GetFor1Device(), cancellationToken), cancellationToken);
        var receiveTask = Task.Run(async () => await _pongService.ReceivePackageAndReturnItAsync(streamCrossover.GetFor2Device(), cancellationToken), cancellationToken);

        await Task.WhenAll(sendTask, receiveTask);

        using var reader = new StreamReader(streamCrossover.GetFor1Device(), Encoding.ASCII, leaveOpen: true);

        // Validate the result
        var result = await reader.ReadToEndAsync(cancellationToken);
        result.Should().Contain("PONG\n", "because the stream should contain the 'PONG' message");
    }
}
