using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;

public class SerialPongSettings(ISerialPortEnumerator portEnumerator) : BaseSerialPingSettings(portEnumerator)
{
}
