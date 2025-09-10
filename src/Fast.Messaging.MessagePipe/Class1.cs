using System.Collections.Concurrent;
using MessagePipe;

namespace Fast.Messaging.MessagePipe;

public sealed class MpNamedPipeClient : IMessageClient<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly ISubscriber<string, ReadOnlyMemory<byte>> _subscriber;
	private readonly IPublisher<string, ReadOnlyMemory<byte>> _publisher;
	private readonly Func<ReadOnlyMemory<byte>, string> _topicSelector; // how to route outgoing messages
	private readonly ConcurrentDictionary<string, IDisposable> _subs = new();

	// publish topic decided per-message
	public MpNamedPipeClient(ISubscriber<string, ReadOnlyMemory<byte>> subscriber,
							 IPublisher<string, ReadOnlyMemory<byte>> publisher,
							 Func<ReadOnlyMemory<byte>, string> topicSelector)
	{
		ArgumentNullException.ThrowIfNull(subscriber);
		ArgumentNullException.ThrowIfNull(publisher);
		ArgumentNullException.ThrowIfNull(topicSelector);

		_subscriber = subscriber;
		_publisher = publisher;
		_topicSelector = topicSelector;
	}

	// OR: fixed outgoing topic
	public MpNamedPipeClient(ISubscriber<string, ReadOnlyMemory<byte>> subscriber,
							 IPublisher<string, ReadOnlyMemory<byte>> publisher,
							 string fixedPublishTopic)
		: this(subscriber, publisher, _ => fixedPublishTopic ?? throw new ArgumentNullException(nameof(fixedPublishTopic))) { }

	public ValueTask ConnectAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

	public ValueTask PublishAsync(ReadOnlyMemory<byte> message, CancellationToken ct = default)
	{
		_publisher.Publish(_topicSelector(message), message);
		return ValueTask.CompletedTask;
	}

	public ValueTask<IDisposable> SubscribeAsync(ISubscription<ReadOnlyMemory<byte>> subscription, CancellationToken ct = default)
	{
		var token = _subscriber.Subscribe(subscription.Topic, (ReadOnlyMemory<byte> m) => subscription.ProcessAsync(m, ct));

		// replace existing sub for same topic (if any)
		if (_subs.TryGetValue(subscription.Topic, out var old)) { old.Dispose(); _subs[subscription.Topic] = token; }
		else _subs.TryAdd(subscription.Topic, token);

		return ValueTask.FromResult<IDisposable>(token);
	}

	public ValueTask<IDisposable> UnSubscribeAsync(string topic, CancellationToken ct = default)
	{
		if (_subs.TryRemove(topic, out var d)) { d.Dispose(); return ValueTask.FromResult(d); }
		return ValueTask.FromResult<IDisposable>(NoopDisposable.Instance);
	}

	public ValueTask DisposeAsync()
	{
		foreach (var d in _subs.Values) d.Dispose();
		_subs.Clear();
		return ValueTask.CompletedTask;
	}

	private sealed class NoopDisposable : IDisposable { public static readonly NoopDisposable Instance = new(); public void Dispose() { } }
}