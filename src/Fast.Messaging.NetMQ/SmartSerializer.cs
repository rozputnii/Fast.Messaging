using System.Buffers;
using MemoryPack;

namespace Fast.Messaging.NetMQ;

public static class SmartSerializer
{
    public static IMemoryOwner<byte> Serialize<T>(T value, int? sizeHint = null)
    {
        var typeKey = BufferSizeCalculator.GetTypeKey<T>(sizeHint);
        var capacity = BufferSizeCalculator.GetOptimalSize(typeKey);

        using var writer = new DotNext.Buffers.PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        MemoryPackSerializer.Serialize(writer, value);

        BufferSizeCalculator.RecordSize(typeKey, writer.WrittenCount);
        return writer.DetachBuffer();
    }
}