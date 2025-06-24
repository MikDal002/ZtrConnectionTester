using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Tests.Commands.SerialPing;

[TestFixture]
public class ReadLineWithTimeoutAsyncTests
{
    [Test]
    [CancelAfter(2000)]
    public Task ReadLineWithTimeoutAsync_ShouldThrowTimeoutException_WhenTimeoutOccurs()
    {
        // Arrange
        using var memoryStream = new MemoryStream(); // Empty stream to simulate no input
        var cancellationTokenSource = new CancellationTokenSource();
        // Act & Assert
        var exception = Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await memoryStream.ReadLineWithTimeoutAsync(timeout: TimeSpan.FromMilliseconds(100), cancellationToken: cancellationTokenSource.Token);
        });
        Assert.That(exception.Message, Does.Contain("Waiting for the incoming line took over"));
        return Task.CompletedTask;
    }
}
