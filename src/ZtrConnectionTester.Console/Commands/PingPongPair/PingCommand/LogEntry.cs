using System;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public record LogEntry(DateTime Timestamp, string Message, LogLevel Level, string? Source = null);