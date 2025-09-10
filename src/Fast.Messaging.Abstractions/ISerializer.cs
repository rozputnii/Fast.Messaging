using System.Buffers;

namespace Fast.Messaging;

public interface ISerializer
{
    T? Deserialize<T>(ReadOnlySequence<byte> payload);

    ReadOnlySequence<byte> Serialize<T>(T value);
}