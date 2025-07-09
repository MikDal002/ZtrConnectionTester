namespace ZtrConnectionTester.Console.Tests.TestStream;

public class StreamCrossover
{
    readonly Stream _first = new MemoryStream();
    readonly Stream _second = new MemoryStream();

    public Stream GetFor1Device()
    {
        return new CrossedStream(_first, _second);
    }

    public Stream GetFor2Device()
    {
        return new CrossedStream(_second, _first);
    }
}
