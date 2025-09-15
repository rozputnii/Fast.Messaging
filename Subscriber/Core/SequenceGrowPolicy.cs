using System.Runtime.CompilerServices;

namespace Poc.Core;

static class SequenceGrowPolicy<T>
{
	public const int MaximumAutoGrowSize = 32 * 1024; // bytes
	public static readonly int DefaultLengthFromArrayPool = 1 + (4095 / Unsafe.SizeOf<T>());

	public static int ConsiderMinimumSizeIncrease(int currentMinElems, long totalElems, bool autoIncrease = true)
	{
		if (!autoIncrease) return currentMinElems;
		int maxMinElems = MaximumAutoGrowSize / Unsafe.SizeOf<T>();
		if (currentMinElems >= maxMinElems) return currentMinElems;
		long target = Math.Min(int.MaxValue, totalElems / 2);
		int t = (int)Math.Clamp(target, currentMinElems, maxMinElems);
		return t > currentMinElems ? t : currentMinElems;
	}

	public static int NextRentSize(int sizeHintElems, int minimumSpanElems)
	{
		// We always rent a slab sized to max(minimum, hint) (no "last segment" concept here).
		if (sizeHintElems <= 0) return minimumSpanElems;
		return Math.Max(minimumSpanElems, sizeHintElems);
	}
}