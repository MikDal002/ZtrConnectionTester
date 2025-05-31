using Spectre.Console;
using System;
using System.IO.Ports;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

public static class SerialPortExtensions
{
    public static SerialPort OpenPortWithVisualSummary(this BaseSerialPingSettings settings, IAnsiConsole console)
    {
        var port = new SerialPort(settings.Port, settings.BaudRate)
        {
            //ReadTimeout = settings.TimeoutMs,
            //WriteTimeout = settings.TimeoutMs,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            //NewLine = "\n"
        };

        port.Open();

        if (!port.IsOpen)
        {
            console.MarkupLine(
                $"[red]Error: Port {settings.Port.EscapeMarkup()} reported as not open after Open() call.[/]");
            port.Dispose();
            throw new InvalidOperationException($"Cannot open port {settings.Port.EscapeMarkup()}");
        }

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(4))
            .AddColumn()
            .AddRow("[bold]Port:[/]", $"[aqua]{settings.Port.EscapeMarkup()}[/]")
            .AddRow("[bold]Baud Rate:[/]", $"[aqua]{port.BaudRate}[/]")
            .AddRow("[bold]Data Bits:[/]", $"[aqua]{port.DataBits}[/]")
            .AddRow("[bold]Parity:[/]", $"[aqua]{port.Parity}[/]")
            .AddRow("[bold]Stop Bits:[/]", $"[aqua]{port.StopBits}[/]")
            .AddRow("[bold]Handshake:[/]", $"[aqua]{port.Handshake}[/]");
        //.AddRow("[bold]Read Timeout:[/]", $"[aqua]{settings.TimeoutMs} ms[/]")
        //.AddRow("[bold]Write Timeout:[/]", $"[aqua]{settings.TimeoutMs} ms[/]")
        //.AddRow("[bold]Payload Size:[/]", $"[aqua]{settings.PayloadSize} bytes[/]")
        //.AddRow("[bold]Packet Size:[/]", $"[aqua]{settings.PayloadSize + 1} bytes (Payload+Checksum)[/]")
        //.AddRow("[bold]Delay:[/]", $"[aqua]{settings.DelayMs} ms[/]");

        console.Write(
            new Panel(grid)
                .Header("Configuration")
                .Border(BoxBorder.Rounded)
                .Expand());
        console.WriteLine();

        return port;
    }
}
