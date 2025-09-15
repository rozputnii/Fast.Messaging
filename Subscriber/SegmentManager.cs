namespace Poc;

using System;
using System.Collections.Generic;

//public sealed class SegmentManager
//{
//	private struct Block { public int Start, End; } // [Start,End)

//	private readonly Block[] _free; // sorted by Start
//	private int _count;
//	private int _cursor;            // next-fit scan start

//	public int Capacity { get; }

//	public SegmentManager(int capacity, int maxFreeSegments = 2048)
//	{
//		if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
//		if (maxFreeSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maxFreeSegments));
//		Capacity = capacity;
//		_free = new Block[maxFreeSegments];
//		_free[0] = new Block { Start = 0, End = capacity };
//		_count = 1;
//		_cursor = 0;
//	}

//	public bool TryRent(int length, out Range rented)
//	{
//		rented = default;
//		if (length <= 0 || length > Capacity) return false;

//		int i = _cursor, n = _count;

//		// pass 1: [cursor..end)
//		for (; i < n; i++)
//		{
//			ref var b = ref _free[i];
//			int len = b.End - b.Start;
//			if (len < length) continue;

//			int s = b.Start, e = s + length;
//			rented = s..e;

//			if (len == length)
//			{
//				RemoveAt(i);
//				if (_cursor > i) _cursor--;
//				if (_cursor >= _count) _cursor = 0;
//			}
//			else
//			{
//				b.Start = e;
//				_cursor = i;
//			}
//			return true;
//		}

//		// pass 2: [0..cursor)
//		for (i = 0, n = _cursor; i < n; i++)
//		{
//			ref var b = ref _free[i];
//			int len = b.End - b.Start;
//			if (len < length) continue;

//			int s = b.Start, e = s + length;
//			rented = s..e;

//			if (len == length)
//			{
//				RemoveAt(i);
//				if (_cursor > i) _cursor--;
//				if (_cursor >= _count) _cursor = 0;
//			}
//			else
//			{
//				b.Start = e;
//				_cursor = i;
//			}
//			return true;
//		}

//		return false;
//	}

//	public bool TryExtend(Range current, int newLength, out Range extended)
//	{
//		extended = current;

//		int start = current.Start.GetOffset(Capacity);
//		int end = current.End.GetOffset(Capacity);
//		if (start < 0 || end > Capacity || start >= end) return false;
//		if (newLength <= 0) return false;

//		int oldLen = end - start;
//		if (newLength == oldLen) { extended = current; return true; }
//		if (newLength < oldLen) return false;

//		int need = newLength - oldLen;

//		int idx = LowerBoundByStart(end);
//		if (idx >= _count) return false;

//		ref var right = ref _free[idx];
//		if (right.Start != end) return false;

//		int avail = right.End - right.Start;
//		if (avail < need) return false;

//		int newEnd = end + need;
//		if (avail == need)
//		{
//			RemoveAt(idx);
//			if (_cursor > idx) _cursor--;
//			if (_cursor >= _count) _cursor = 0;
//		}
//		else
//		{
//			right.Start += need;
//			_cursor = idx;
//		}

//		extended = start..newEnd;
//		return true;
//	}

//	public void Return(Range range)
//	{
//		int start = range.Start.GetOffset(Capacity);
//		int end = range.End.GetOffset(Capacity);
//		if (start < 0 || end > Capacity || start >= end)
//			throw new ArgumentOutOfRangeException(nameof(range));

//		int idx = LowerBoundByStart(start);

//		// left neighbor merge / checks
//		if (idx > 0)
//		{
//			ref var left = ref _free[idx - 1];
//			if (left.Start <= start && end <= left.End) return;                 // double free subset
//			if (left.End > start) throw new InvalidOperationException("Overlap");
//			if (left.End == start) { start = left.Start; RemoveAt(idx - 1); idx--; }
//		}

//		// right neighbor merge / checks
//		if (idx < _count)
//		{
//			ref var right = ref _free[idx];
//			if (right.Start < end) throw new InvalidOperationException("Overlap");
//			if (right.Start == end) { end = right.End; RemoveAt(idx); }
//		}

//		InsertAt(idx, start, end);
//	}

//	// --- helpers (no allocations) ---

//	private int LowerBoundByStart(int start)
//	{
//		int lo = 0, hi = _count;
//		while (lo < hi)
//		{
//			int mid = (lo + hi) >> 1;
//			if (_free[mid].Start < start) lo = mid + 1; else hi = mid;
//		}
//		return lo;
//	}

//	private void InsertAt(int idx, int start, int end)
//	{
//		if (_count == _free.Length) throw new InvalidOperationException("Free list full");
//		int move = _count - idx;
//		if (move > 0) Array.Copy(_free, idx, _free, idx + 1, move);
//		_free[idx] = new Block { Start = start, End = end };
//		_count++;
//		if (idx <= _cursor) _cursor++;
//	}

//	private void RemoveAt(int i)
//	{
//		int tail = _count - i - 1;
//		if (tail > 0) Array.Copy(_free, i + 1, _free, i, tail);
//		_count--;
//		if (_cursor > i) _cursor--;
//		if (_cursor >= _count) _cursor = 0;
//	}
//}

//public sealed class SegmentManager
//{
//	private readonly SortedSet<Range> _byStart;
//	private readonly SortedSet<Range> _bySize;

//	public int Capacity { get; }

//	public SegmentManager(int capacity)
//	{
//		if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
//		Capacity = capacity;

//		_byStart = new(SortByStart);
//		_bySize = new(SortBySizeThenStart);

//		var all = 0..capacity;   // one big free block
//		AddFree(all);
//	}

//	public bool TryRent(int length, out Range rented)
//	{
//		rented = default;
//		if (length <= 0 || length > Capacity) return false;

//		// find smallest free block >= length
//		var sentinel = 0..length;
//		var view = _bySize.GetViewBetween(sentinel, SizeUpperSentinel);
//		if (view.Count == 0) return false;

//		var block = view.Min;
//		RemoveFree(block);

//		int start = block.Start.Value;
//		int end = start + length;
//		rented = start..end;

//		// return remainder tail back to free list
//		if (end < block.End.Value)
//			AddFree(end..block.End.Value);

//		return true;
//	}

//	public bool TryExtend(Range current, int newLength, out Range extended)
//	{
//		int start = current.Start.GetOffset(Capacity);
//		int end = current.End.GetOffset(Capacity);
//		extended = current;

//		if (start < 0 || end > Capacity || start >= end) return false;
//		if (newLength <= 0) return false;

//		int oldLen = end - start;
//		if (newLength == oldLen) return true;          // no-op
//		if (newLength < oldLen) return false;         // shrink not supported
//		if (start + newLength > Capacity) return false;

//		int need = newLength - oldLen;

//		// find free block immediately to the right
//		var rightView = _byStart.GetViewBetween(end..end, Capacity..Capacity);
//		if (rightView.Count == 0) return false;

//		var right = rightView.Min;
//		if (right.Start.Value != end) return false;    // not adjacent → can't extend

//		int freeLen = right.End.Value - right.Start.Value;
//		if (freeLen < need) return false;              // not enough space

//		RemoveFree(right);

//		int newEnd = end + need;
//		if (need < freeLen)                            // put back tail of the free block
//			AddFree(newEnd..right.End.Value);

//		extended = start..newEnd;
//		return true;
//	}


//	public void Return(Range range)
//	{
//		int start = range.Start.GetOffset(Capacity);
//		int end = range.End.GetOffset(Capacity);
//		if (start < 0 || end > Capacity || start >= end)
//			throw new ArgumentOutOfRangeException(nameof(range));

//		// neighbors
//		var leftView = _byStart.GetViewBetween(0..0, start..start);
//		var hasLeft = leftView.Count != 0;
//		var left = hasLeft ? leftView.Max : default;

//		var rightView = _byStart.GetViewBetween(start..start, Capacity..Capacity);
//		var hasRight = rightView.Count != 0;
//		var right = hasRight ? rightView.Min : default;

//		// already free (subset of left) -> no-op (double free)
//		if (hasLeft && left.Start.Value <= start && end <= left.End.Value)
//			return;

//		// prevent corrupting state by overlapping existing free blocks
//		if (hasLeft && left.End.Value > start) throw new InvalidOperationException("Overlapping free (double free).");
//		if (hasRight && right.Start.Value < end) throw new InvalidOperationException("Overlapping free (double free).");

//		// merge only on exact touch
//		if (hasLeft && left.End.Value == start) { RemoveFree(left); start = left.Start.Value; }
//		if (hasRight && right.Start.Value == end) { RemoveFree(right); end = right.End.Value; }

//		AddFree(start..end);
//	}

//	// ---- helpers ----
//	private void AddFree(Range r)
//	{
//		_byStart.Add(r);
//		_bySize.Add(r);
//	}

//	private void RemoveFree(Range r)
//	{
//		_byStart.Remove(r);
//		_bySize.Remove(r);
//	}

//	private static int Len(in Range r) => r.End.Value - r.Start.Value;

//	private static readonly Comparer<Range> SortByStart =
//		Comparer<Range>.Create((a, b) =>
//		{
//			int c = a.Start.Value.CompareTo(b.Start.Value);
//			return c != 0 ? c : a.End.Value.CompareTo(b.End.Value);
//		});

//	private static readonly Comparer<Range> SortBySizeThenStart =
//		Comparer<Range>.Create((a, b) =>
//		{
//			int c = Len(a).CompareTo(Len(b));
//			if (c != 0) return c;
//			c = a.Start.Value.CompareTo(b.Start.Value);
//			return c != 0 ? c : a.End.Value.CompareTo(b.End.Value);
//		});

//	private static readonly Range SizeUpperSentinel =
//		new(new Index(0, false), new Index(int.MaxValue, false));
//}
