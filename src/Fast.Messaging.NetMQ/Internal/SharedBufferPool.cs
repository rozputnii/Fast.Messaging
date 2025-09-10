using System.Buffers;
using NetMQ;

namespace Fast.Messaging.Internal;

internal sealed class SharedBufferPool : IBufferPool
{
    public void Dispose() { }
    public byte[] Take(int size) => ArrayPool<byte>.Shared.Rent(size);
    public void Return(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);
}