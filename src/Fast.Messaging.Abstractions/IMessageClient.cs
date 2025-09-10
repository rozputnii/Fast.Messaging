using System.Buffers;

namespace Fast.Messaging;

public interface IMessageClient : IAsyncDisposable
{
    ValueTask ConnectAsync(CancellationToken ct = default);

    ValueTask PublishAsync(string topic, ReadOnlySequence<byte> message, CancellationToken ct = default);

    ValueTask<IDisposable> SubscribeAsync(ISubscription subscription, CancellationToken ct = default);

    ValueTask<IDisposable> UnSubscribeAsync(string topic, CancellationToken ct = default);
}


//public interface ISubscription<in TMessage>
//{
//    string Topic { get; }

//    ValueTask ProcessAsync(TMessage message, CancellationToken ct = default);
//}

public interface ISubscription
{
    string Topic { get; }

    /// <summary>
    /// Automatically reconnects until ct canceled. Automatically subscribes to Topic upon connection.
    /// Process messages by invoking the provided callback.
    /// </summary>
    Task ProcessAsync(Func<ReadOnlySequence<byte>, ValueTask> callback, CancellationToken ct = default);
}

public interface IPublication
{
    string Topic { get; }

	/// <summary>
	/// Long-running task to accept subscribers.
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task RunAsync(CancellationToken ct = default);
    ValueTask PublishAsync(ReadOnlySequence<byte> payload, CancellationToken ct = default);
}