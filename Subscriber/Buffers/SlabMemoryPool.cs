using Poc.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

public sealed class SlabMemoryPool<T> : MemoryPool<T>
{
	private readonly ArrayPool<T> _pool;
	private readonly Dictionary<int, Slab?> _buckets = new(); // key = slab size (elements)
	private readonly Lock _bucketsLock = new();

	private int _minSpanElems = SequenceGrowPolicy<T>.DefaultLengthFromArrayPool;
	private long _allocatedElems;

	public SlabMemoryPool(ArrayPool<T>? pool = null) => _pool = pool ?? ArrayPool<T>.Shared;

	public override int MaxBufferSize => int.MaxValue;

	public override IMemoryOwner<T> Rent(int size = -1)
	{
		int hintElems = size > 0 ? size : 0;
		int slabElems = SequenceGrowPolicy<T>.NextRentSize(hintElems, Volatile.Read(ref _minSpanElems));

		// try existing slabs in this bucket (read head under lock)
		Slab? head;
		using (_bucketsLock.EnterScope())
			_buckets.TryGetValue(slabElems, out head);

		for (var s = head; s != null; s = s.Next)
			if (s.Map.TryRent(hintElems, out var slice))
				return NewOwner(s, slice);

		// create + link as new head
		var arr = _pool.Rent(slabElems);
		var slab = new Slab(arr, slabElems);

		using (_bucketsLock.EnterScope())
		{
			_buckets.TryGetValue(slabElems, out head);
			slab.Next = head;
			_buckets[slabElems] = slab;
		}

		if (!slab.Map.TryRent(hintElems, out var first))
			throw new InvalidOperationException("Rent failed on fresh slab.");

		return NewOwner(slab, first);

		BlockOwner NewOwner(Slab s, SliceMap64.Slice slice)
		{
			int len = slice.LengthElements;
			Interlocked.Add(ref _allocatedElems, len);
			Volatile.Write(ref _minSpanElems,
				SequenceGrowPolicy<T>.ConsiderMinimumSizeIncrease(_minSpanElems, _allocatedElems));
			return new BlockOwner(s, slice, len, OnReturn);
		}
	}

	protected override void Dispose(bool disposing)
	{
		using (_bucketsLock.EnterScope())
		{
			foreach (var kv in _buckets)
				for (var s = kv.Value; s != null; s = s.Next)
					_pool.Return(s.Buffer, clearArray: false);

			_buckets.Clear();
		}
	}

	private void OnReturn(int lengthElems) => Interlocked.Add(ref _allocatedElems, -lengthElems);

	private sealed class Slab
	{
		public readonly T[] Buffer;
		public readonly int UsableElements;
		public readonly SliceMap64 Map;
		public Slab? Next;

		public Slab(T[] buffer, int usableElements)
		{
			Buffer = buffer;
			UsableElements = usableElements;
			Map = new SliceMap64(usableElements);
		}
	}

	private sealed class BlockOwner : IMemoryOwner<T>
	{
		private Slab? _slab;
		private readonly SliceMap64.Slice _slice;
		private readonly int _lenElems;
		private readonly Action<int> _onReturn;

		public BlockOwner(Slab slab, SliceMap64.Slice slice, int lenElems, Action<int> onReturn)
		{ _slab = slab; _slice = slice; _lenElems = lenElems; _onReturn = onReturn; }

		public Memory<T> Memory
		{
			get
			{
				var s = _slab ?? throw new ObjectDisposedException(nameof(BlockOwner));
				return new Memory<T>(s.Buffer, _slice.OffsetElements, _lenElems);
			}
		}

		public void Dispose()
		{
			var s = Interlocked.Exchange(ref _slab, null);
			if (s is null) return;
			s.Map.Return(_slice);
			_onReturn(_lenElems);
		}
	}
}
