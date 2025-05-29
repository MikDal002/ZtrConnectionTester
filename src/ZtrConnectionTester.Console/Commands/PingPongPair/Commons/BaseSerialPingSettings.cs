using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Linq;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

public abstract class BaseSerialPingSettings(ISerialPortEnumerator portEnumerator) : CommandSettings
{
    [CommandOption("-p|--port <PORT>")]
    [Description("Required. The serial port name (e.g., /dev/ttyS0 or COM3).")]
    public string Port { get; set; } = string.Empty;

    [CommandOption("-b|--baud <BAUD_RATE>")]
    [Description("The baud rate for serial communication.")]
    [DefaultValue(115200)]
    public int BaudRate { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Port))
        {
            return ValidationResult.Error("The --port option is required.");
        }

        var availablePorts = portEnumerator
            .GetAvailablePortNames()
            .ToList();

        if (!availablePorts.Contains(Port, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Error(
                $"Serial port '{Port}' not found. Available ports: {string.Join(", ", availablePorts)}");
        }

        if (BaudRate <= 0)
        {
            return ValidationResult.Error("Baud rate must be a positive integer.");
        }

        // Basic validation successful for base properties
        return ValidationResult.Success();
    }
}
