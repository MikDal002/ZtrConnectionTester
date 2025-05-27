using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrConnectionTester.Console.Commands.Base;
using ZtrConnectionTester.Console.Infrastructure;

namespace ZtrConnectionTester.Console;

public static class SerialPingDependencyInjection
{
    public static IServiceCollection RegisterSerialPingCommand(this IServiceCollection services)
    {
        services.AddSingleton<ISerialPortEnumerator, DefaultSerialPortEnumerator>();

        return services;
    }
}

public class SerialPingSettings(ISerialPortEnumerator portEnumerator) : CommandSettings
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



public class SerialPingCommand(IAnsiConsole console, ILogger<SerialPingCommand> logger) : CancellableAsyncCommand<SerialPingSettings>
{
    private readonly ILogger<SerialPingCommand> _logger = logger;

    public override Task<int> ExecuteAsync(CommandContext context, SerialPingSettings settings, CancellationToken cancellationToken)
    {
        console.MarkupLine("[yellow]Press Ctrl+C to stop[/]");
        using var serialPort = OpenPort(settings);



        throw new Exception();
    }

    private SerialPort OpenPort(SerialPingSettings settings)
    {
        logger.LogInformation(
            "Attempting to open port {SerialPort} at {SerialBaudRate} baud...", settings.Port, settings.BaudRate);
        throw new Exception();
    }
}
