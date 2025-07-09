using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public class SerialPingSettings(ISerialPortEnumerator portEnumerator) : BaseSerialPingSettings(portEnumerator)
{

}
