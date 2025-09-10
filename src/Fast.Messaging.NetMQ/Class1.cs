using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using MemoryPack;

namespace Fast.Messaging.NetMQ;

public static class Extensions
{
	public static IMemoryOwner<byte> ToBytes<T>(this T value) => SmartSerializer.Serialize(value);
	public static IMemoryOwner<byte> ToBytes<T>(this ICollection<T> collection) => SmartSerializer.Serialize(collection, collection.Count);
	public static IMemoryOwner<byte> ToBytes<T>(this T[] array) => SmartSerializer.Serialize(array, array.Length);

	public static T FromBytes<T>(this ReadOnlyMemory<byte> memory) => MemoryPackSerializer.Deserialize<T>(memory.Span);
	public static T FromBytes<T>(this IMemoryOwner<byte> owner)
	{
		try { return MemoryPackSerializer.Deserialize<T>(owner.Memory.Span); }
		finally { owner.Dispose(); }
	}
}

