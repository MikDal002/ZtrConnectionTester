namespace ZtrConnectionTester.Console.Tests.TestStream;

public class CrossedStream : Stream
{
    readonly Stream _readStream;
    readonly Stream _writeStream;

    public CrossedStream(Stream readStream, Stream writeStream)
    {
        _readStream = readStream;
        _writeStream = writeStream;
    }

    public override void Flush()
    {
        _readStream.Flush();
        _writeStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _readStream.Read(buffer, offset, count);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException("Seeking is not supported in ConsumableStream.");

    public override void SetLength(long value)
        => throw new NotSupportedException("Setting length is not supported in ConsumableStream.");

    public override void Write(byte[] buffer, int offset, int count)
    {
        _writeStream.Write(buffer, offset, count);
        _writeStream.Position -= count;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        _writeStream.Position -= count;
    }

    public override bool CanRead => _readStream.CanRead;
    public override bool CanSeek => false; // Seeking is not supported
    public override bool CanWrite => _writeStream.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
