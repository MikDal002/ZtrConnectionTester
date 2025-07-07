namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public class PingResult
{
    public PingResultStatus Status { get; }
    public double LatencyMs { get; } // Only valid if Status is Success
    public int BytesReceived { get; } // Number of bytes received for successful pings
    public string? ErrorMessage { get; } // For IoError or potentially Corrupted

    private PingResult(PingResultStatus status, double latencyMs = 0, int bytesReceived = 0, string? errorMessage = null)
    {
        Status = status;
        LatencyMs = latencyMs;
        BytesReceived = bytesReceived;
        ErrorMessage = errorMessage;
    }

    public static PingResult SuccessResult(double latencyMs, int bytesReceived)
    {
        return new(PingResultStatus.Success, latencyMs, bytesReceived);
    }

    public static PingResult TimeoutResult()
    {
        return new(PingResultStatus.Timeout);
    }

    public static PingResult CorruptedResult(string? details = null)
    {
        return new(PingResultStatus.Corrupted, errorMessage: details);
    }

    public static PingResult IoErrorResult(string message)
    {
        return new(PingResultStatus.IoError, errorMessage: message);
    }

    public static PingResult CancelledResult()
    {
        return new(PingResultStatus.Cancelled);
        // Added factory method
    }
}