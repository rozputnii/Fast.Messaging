using System.Numerics;
using System.Runtime.CompilerServices;

namespace Poc.Core;

public static class BitHelpers64
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong Mask(int count)
		=> count >= 64 ? ulong.MaxValue : ((1UL << count) - 1UL);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsRangeFree(ulong word, int start, int count)
		=> (uint)(start + count) <= 64u && ((word >> start) & Mask(count)) == 0;

	// Lock-free: find any run of 'count' zero bits and mark them 1s via CAS.
	public static bool TryMarkFreeRun(ref ulong word, int count, out int start)
	{
		if ((uint)count - 1u >= 64u) { start = -1; return false; }

		while (true)
		{
			ulong old = Volatile.Read(ref word);
			ulong free = ~old;

			// find run of 'count' ones in 'free'
			ulong run = free;
			for (int k = 1; k < count; k++) run &= (free << k);
			if (count == 64) run = free == ulong.MaxValue ? 1UL : 0UL;

			if (run == 0) { start = -1; return false; }
			int pos = BitOperations.TrailingZeroCount(run);
			ulong mask = Mask(count) << pos;

			ulong @new = old | mask;
			if (Interlocked.CompareExchange(ref word, @new, old) == old)
			{ start = pos; return true; }
		}
	}

	// Lock-free clear of a known range.
	public static void ClearRange(ref ulong word, int start, int count)
	{
		if ((uint)(start + count) > 64u) throw new ArgumentOutOfRangeException();
		ulong mask = Mask(count) << start;
		while (true)
		{
			ulong old = Volatile.Read(ref word);
			ulong @new = old & ~mask;
			if (Interlocked.CompareExchange(ref word, @new, old) == old) return;
		}
	}
}