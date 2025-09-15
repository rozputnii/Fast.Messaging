using System.Buffers;
using CommunityToolkit.HighPerformance.Buffers;
using DotNext.Buffers;
using MemoryPack;

namespace Fast.Messaging.NetMQ;

public static class SmartSerializer
{
    public static IMemoryOwner<byte> Serialize<T>(T value, int? sizeHint = null)
    {
        var writer2 = new CommunityToolkit.HighPerformance.Buffers.ArrayPoolBufferWriter<byte>(ArrayPool<byte>.Shared);
		using var writer = new DotNext.Buffers.PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        MemoryPackSerializer.Serialize(writer, value);
        writer.DetachBuffer()

		return writer.DetachBuffer();
    }
}