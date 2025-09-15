// BenchmarkDotNet: dotnet add package BenchmarkDotNet
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Poc;
using System;
using System.Collections.Generic;

BenchmarkRunner.Run<SegmentManagerBenchmarks>();

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput)]
public class SegmentManagerBenchmarks
{
	[Params(32 * 1024)]
	public int Capacity;

	[Params(16, 64)]
	public int Chunk;

	private SegmentManager _mgr;
	private readonly List<Range> _rents = new();

	[GlobalSetup]
	public void Setup()
	{
		_mgr = new SegmentManager(Capacity);
		_rents.Capacity = Capacity / Math.Max(1, Chunk);
	}

	// Rent until full, then return all — measures allocator hot path + coalescing
	[Benchmark]
	public int RentReturn_Churn()
	{
		_rents.Clear();
		int count = 0;

		while (_mgr.TryRent(Chunk, out var r))
		{
			_rents.Add(r);
			count++;
		}
		for (int i = 0; i < _rents.Count; i++)
			_mgr.Return(_rents[i]);

		return count;
	}

	// Ensure full, extra rent should fail — measures failed fast path
	[Benchmark]
	public bool Rent_FailWhenFull()
	{
		var local = new SegmentManager(Capacity);
		while (local.TryRent(Chunk, out _)) { }
		return local.TryRent(Chunk, out _); // expected false
	}

	// Extend succeeds into adjacent free block (best-fit split + extend)
	[Benchmark]
	public bool Extend_Success_AdjacentRight()
	{
		// layout: [A=Chunk][Free=Capacity-Chunk]
		var local = new SegmentManager(Capacity);
		local.TryRent(Chunk, out var a);
		local.TryRent(Capacity - Chunk, out var tmp);
		local.Return(tmp); // make right side free & adjacent
		return local.TryExtend(a, Chunk * 2, out _); // requires 2*Chunk <= Capacity
	}

	// Extend fails due to right neighbor occupying space
	[Benchmark]
	public bool Extend_Fail_BlockedRight()
	{
		var local = new SegmentManager(Capacity);
		local.TryRent(Chunk, out var a);
		local.TryRent(Chunk, out _); // block directly to the right
		return local.TryExtend(a, Chunk * 2, out _); // expected false
	}

	// Mixed: rent half, free every second, rent again — fragmentation/merge behavior
	[Benchmark]
	public int Fragmentation_Rent_Free_Rent()
	{
		var local = new SegmentManager(Capacity);
		var list = new List<Range>(Capacity / Math.Max(1, Chunk));

		// fill
		while (local.TryRent(Chunk, out var r)) list.Add(r);

		// free every second
		for (int i = 0; i < list.Count; i += 2) local.Return(list[i]);

		// try rent again same chunk size
		int gained = 0;
		while (local.TryRent(Chunk, out _)) gained++;

		return gained;
	}
}


//using System;
//using System.Buffers;
//using MemoryPack;

//// ---- custom writer ----
//static void SerializeAndDebug<T>(T model)
//{
//	Console.WriteLine($"\n--- {typeof(T).Name} {model}---");
//	//using var writer = new SimpleBufferWriter();
//	using var writer = new DotNext.Buffers.PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);


//	MemoryPackSerializer.Serialize(writer, model);
//	//Console.WriteLine($"{writer}, {writer.WrittenArray.Count}");
//}

//SerializeAndDebug(1);
//SerializeAndDebug(1L);
//SerializeAndDebug(1D);
//SerializeAndDebug('1');
//SerializeAndDebug("12");
//SerializeAndDebug(new SmallModel { Id = 1, Name = "Test" });
//SerializeAndDebug(new MediumModel());
//SerializeAndDebug(new LargeModel());
//Console.ReadLine();

//return;

//public sealed class SimpleBufferWriter : IBufferWriter<byte>, IDisposable
//{
//	private byte[]? _buffer;
//	private int _position;

//	public void Advance(int count)
//	{
//		_position += count;
//		Console.WriteLine($"[Advance] +{count}, pos={_position}");
//	}

//	public Memory<byte> GetMemory(int size = 0)
//	{
//		Console.WriteLine($"[GetMemory] {size}");
//		EnsureCapacity(size);

//		return _buffer!.AsMemory(_position);
//	}

//	public Span<byte> GetSpan(int size = 0)
//	{
//		Console.WriteLine($"[GetSpan] {size}");
//		EnsureCapacity(size);

//		return _buffer!.AsSpan(_position);
//	}

//	private void EnsureCapacity(int sizeHint)
//	{
//		sizeHint = sizeHint == 0 ? 1 : sizeHint;

//		if (_buffer == null)
//		{
//			_buffer = ArrayPool<byte>.Shared.Rent(sizeHint);
//			Console.WriteLine($"[Rent] {_buffer.Length}");
//			return;
//		}

//		if (_position + sizeHint <= _buffer.Length) return;

//		// Need to grow
//		var newSize = Math.Max(_buffer.Length * 2, _position + sizeHint);
//		var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
//		Console.WriteLine($"[Grow] from {_buffer.Length} to {newBuffer.Length}");
//		_buffer.AsSpan(0, _position).CopyTo(newBuffer);
//		ArrayPool<byte>.Shared.Return(_buffer);
//		_buffer = newBuffer;
//	}

//	public void Dispose()
//	{
//		if (_buffer != null)
//		{
//			ArrayPool<byte>.Shared.Return(_buffer);
//			Console.WriteLine($"[Return] {_buffer.Length}");
//			_buffer = null;
//		}
//	}
//}


//// ---- models ----
//[MemoryPackable]
//public partial record SmallModel
//{
//	public int Id { get; set; }
//	public string Name { get; set; } = "Hi";
//}

//[MemoryPackable]
//public partial record MediumModel
//{
//	public byte[] Data { get; set; } = new byte[8 * 1024]; // 8 KB
//}

//[MemoryPackable]
//public partial record LargeModel
//{
//	public byte[] Data { get; set; } = new byte[100_000]; // >85 KB LOH
//}

//// ---- top-level code ----