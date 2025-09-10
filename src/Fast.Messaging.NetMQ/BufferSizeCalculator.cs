using System.Collections.Concurrent;

namespace Fast.Messaging.NetMQ;

public sealed class BufferSizeCalculator
{
    private static readonly ConcurrentDictionary<string, SizeStats> _stats = new();
    private const int MaxHistory = 50;
    private const double GrowthFactor = 1.25;
    private const int MinSize = 256;
    private const int MaxSize = 65536;

    private sealed class SizeStats
    {
        private readonly Queue<int> _sizes = new();
        private readonly object _lock = new();
        private long _sum;

        public void Record(int size)
        {
            lock (_lock)
            {
                _sizes.Enqueue(size);
                _sum += size;

                while (_sizes.Count > MaxHistory)
                {
                    _sum -= _sizes.Dequeue();
                }
            }
        }

        public int GetOptimalSize()
        {
            lock (_lock)
            {
                if (_sizes.Count == 0) return MinSize;

                var avg = _sum / (double)_sizes.Count;
                var sorted = _sizes.OrderBy(x => x).ToArray();
                var p90 = sorted[(int)(sorted.Length * 0.9)];

                var optimal = (int)(Math.Max(avg, p90) * GrowthFactor);
                return Math.Max(MinSize, Math.Min(MaxSize, optimal));
            }
        }
    }

    public static void RecordSize(string typeKey, int actualSize)
    {
        _stats.GetOrAdd(typeKey, _ => new SizeStats()).Record(actualSize);
    }

    public static int GetOptimalSize(string typeKey)
    {
        return _stats.TryGetValue(typeKey, out var stats) ? stats.GetOptimalSize() : MinSize;
    }

    public static string GetTypeKey<T>(int? sizeHint = null)
    {
        var key = typeof(T).Name;
        if (sizeHint.HasValue)
        {
            var category = sizeHint.Value switch
            {
                <= 10 => "s",
                <= 100 => "m",
                <= 1000 => "l",
                _ => "xl"
            };
            key += $"_{category}";
        }
        return key;
    }
}