using BenchmarkDotNet.Running;
using Benchmarks;

//BenchmarkRunner.Run<BufferWriterBenchmarks>();

var s = new SmallModel2(int.MaxValue, "лтвлаптішвапшои fdghdfghd fghdfg hdfgh dfgh  шиіgfjш", long.MaxValue);
var b = MemoryPack.MemoryPackSerializer.Serialize(s); 
Console.ReadLine();