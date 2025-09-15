using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Poc.Core;

sealed class SliceMap64
{
	public int CapacityElements { get; }
	public int SegmentElements { get; }
	private ulong _used; // 1=taken

	public SliceMap64(int capacityElements)
	{
		if (capacityElements <= 0) throw new ArgumentOutOfRangeException(nameof(capacityElements));
		CapacityElements = capacityElements;
		SegmentElements = Math.Max(1, capacityElements / 64);
	}

	public readonly record struct Slice(int StartSegment, int SegmentCount, int OffsetElements, int LengthElements);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRent(int minLengthElements, out Slice s)
	{
		if (minLengthElements <= 0) minLengthElements = SegmentElements;
		int segs = (minLengthElements + SegmentElements - 1) / SegmentElements;
		if ((uint)segs > 64u) { s = default; return false; }

		if (!BitHelpers64.TryMarkFreeRun(ref _used, segs, out int start))
		{ s = default; return false; }

		int off = start * SegmentElements;
		int len = Math.Min(minLengthElements, segs * SegmentElements);
		s = new Slice(start, segs, off, len);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Return(Slice s) => BitHelpers64.ClearRange(ref _used, s.StartSegment, s.SegmentCount);
}