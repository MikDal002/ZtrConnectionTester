namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public enum PingResultStatus
{
    Success,
    Timeout,
    Corrupted,
    IoError,
    Cancelled // Added for explicit cancellation handling within the executor
}
