using System;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public class PingResult
{
    public PingResultStatus Status { get; }
    public TimeSpan Latency { get; } // Only valid if Status is Success
    public int BytesReceived { get; } // Number of bytes received for successful pings
    public string? ErrorMessage { get; } // For IoError or potentially Corrupted

    private PingResult(PingResultStatus status, TimeSpan? latency = null, int bytesReceived = 0, string? errorMessage = null)
    {
        Status = status;
        Latency = latency ?? TimeSpan.Zero;
        BytesReceived = bytesReceived;
        ErrorMessage = errorMessage;
    }

    public static PingResult SuccessResult(TimeSpan latency, int bytesReceived)
        => new(PingResultStatus.Success, latency, bytesReceived);

    public static PingResult TimeoutResult()
        => new(PingResultStatus.Timeout);

    public static PingResult CorruptedResult(string? details = null)
        => new(PingResultStatus.Corrupted, errorMessage: details);

    public static PingResult IoErrorResult(string message)
        => new(PingResultStatus.IoError, errorMessage: message);

    public static PingResult CancelledResult()
        => new(PingResultStatus.Cancelled);
}
