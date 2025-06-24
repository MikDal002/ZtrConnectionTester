using System.Collections.Generic;
using System.IO.Ports;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

/// <summary>
///     Abstraction for enumerating available serial ports.
/// </summary>
public interface ISerialPortEnumerator
{
    /// <summary>
    ///     Gets an array of serial port names for the current computer.
    /// </summary>
    /// <returns>An array of serial port names.</returns>
    IEnumerable<string> GetAvailablePortNames();
}

public class DefaultSerialPortEnumerator : ISerialPortEnumerator
{
    public IEnumerable<string> GetAvailablePortNames()
    {
        return SerialPort.GetPortNames();
    }
}
