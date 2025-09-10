using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Fast.Messaging.Internal;
internal static class Extensions
{
    public static IEnumerable<(ArraySegment<byte> Segment, bool IsLatest)> GetSegments(this ReadOnlySequence<byte> seq)
    {
        ArraySegment<byte>? prev = null;

        foreach (ReadOnlyMemory<byte> m in seq)
        {
            if (m.IsEmpty) continue;

            if (!MemoryMarshal.TryGetArray(m, out var segment))
                throw new InvalidOperationException("Unable to get array from ReadOnlyMemory");

            if (prev != null)
                yield return (prev.Value, false);

            prev = segment;
        }

        if (prev != null)
            yield return (prev.Value, true);
    }
}
