
using System.Buffers;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;
using DotNext.Buffers;
using MemoryPack;

namespace Benchmarks;

[MemoryPackable]
public partial record SmallModel2(int Id, string Name,long l);

[MemoryDiagnoser]
[GcServer(true)]
public class SmallWriterBenchmarks
{
	private SmallModel2 _small;

	[GlobalSetup]
	public void Setup()
	{
		var _small = new SmallModel2(int.MaxValue, "лтвлаптішвапшои fdghdfghd fghdfg hdfgh dfgh  шиіgfjш", long.MaxValue);
	}

	// ---------------- Toolkit BufferWriter ----------------

	[Benchmark]
	public byte[] Raw()
	{
		return MemoryPackSerializer.Serialize(_small);
	}

	[Benchmark]
	public void Toolkit()
	{
		using var writer = new ArrayPoolBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}

	// ---------------- DotNext BufferWriter ----------------
	[Benchmark]
	public void DotNext()
	{
		using var writer = new PoolingArrayBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}

	// ---------------- DotNext BufferWriter ----------------
	[Benchmark]
	public void Sparse()
	{
		using var writer = new SparseBufferWriter<byte>();
		MemoryPackSerializer.Serialize(writer, _small);
	}
	
	[Benchmark]
	public void BufferWriterSlim()
	{
		var writer = new BufferWriterSlim<byte>(stackalloc byte[128]);
		MemoryPackSerializer.Serialize(ref writer, _small);
	}
}
