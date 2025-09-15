
using System.Buffers;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;
using DotNext.Buffers;
using MemoryPack;

namespace Benchmarks;

[MemoryPackable]
public partial record SmallModel(int Id, string Name);

[MemoryPackable]
public partial record LargeModel(int Id, string Name, byte[] Payload);

[MemoryDiagnoser]
[GcServer(true)]
public class BufferWriterBenchmarks
{
	private SmallModel _small;
	private LargeModel _large;
	private List<LargeModel> _largeList;

	[GlobalSetup]
	public void Setup()
	{
		_small = new SmallModel(42, "Test");
		var bigPayload = new byte[32 * 1024];
		new Random(42).NextBytes(bigPayload);
		_large = new LargeModel(1, "Large", bigPayload);

		_largeList = new List<LargeModel>(2000);
		for (int i = 0; i < _largeList.Capacity; i++)
			_largeList.Add(new LargeModel(i, "L", bigPayload));
	}

	// ---------------- Toolkit BufferWriter ----------------
	[Benchmark]
	public void Toolkit_Small()
	{
		using var writer = new ArrayPoolBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}

	[Benchmark]
	public void Toolkit_Large()
	{
		using var writer = new ArrayPoolBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _large);

	}

	[Benchmark]
	public void Toolkit_LargeList()
	{
		using var writer = new ArrayPoolBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _largeList);
	
	}

	// ---------------- DotNext BufferWriter ----------------
	[Benchmark]
	public void DotNext_Small()
	{
		using var writer = new PoolingArrayBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}

	[Benchmark]
	public void DotNext_Large()
	{
		using var writer = new PoolingArrayBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _large);
	}
	[Benchmark]
	public void DotNext_LargeList()
	{
		using var writer = new PoolingArrayBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _largeList);
	}

	// ---------------- DotNext BufferWriter ----------------
	[Benchmark]
	public void Sparse_Small()
	{
		using var writer = new SparseBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}

	[Benchmark]
	public void Sparse_Large()
	{
		using var writer = new SparseBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _large);
	}
	[Benchmark]
	public void Sparse_LargeList()
	{
		using var writer = new SparseBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _largeList);
	}
}
