using Fast.Messaging.Internal;
using NetMQ;
using NetMQ.Sockets;
using System.Buffers;
using System.Threading.Channels;

namespace Fast.Messaging;

public sealed class Publisher : IAsyncDisposable
{
	static Publisher()
	{
		BufferPool.SetCustomBufferPool(new SharedBufferPool());
	}

	private readonly PublisherSocket _socket;
	private readonly Channel<PublishItem> _publishChannel;
	private readonly ChannelWriter<PublishItem> _channelWriter;
	private readonly Task _publishTask;
	private readonly CancellationTokenSource _disposalCts;
	private volatile bool _disposed;

	// Internal structure for queued publish operations

	private readonly PublisherConfig _config;
	public Publisher(PublisherConfig config, int channelCapacity = 10000)
	{
		_config = config;
		_socket = new PublisherSocket();
		_disposalCts = new CancellationTokenSource();		

		var channelOptions = new BoundedChannelOptions(channelCapacity)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = false
		};

		_publishChannel = Channel.CreateBounded<PublishItem>(channelOptions);
		_channelWriter = _publishChannel.Writer;

		_socket.Bind(_config.Endpoint);
		_socket.Options.SendBuffer = 4 * 1024 * 1024;
		_socket.Options.Linger = TimeSpan.Zero;

		_publishTask = ProcessPublishQueue(_disposalCts.Token);
		_publishTask.Start();
	}

	public async Task<bool> PublishAsync(byte[] topic, ReadOnlySequence<byte> data,
		CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposalCts.Token);

		try
		{
			var item = new PublishItem(topic, data, combinedCts.Token);

			// Queue the publish operation
			if (await _channelWriter.WaitToWriteAsync(combinedCts.Token))
			{
				if (_channelWriter.TryWrite(item))
				{
					// Wait for completion
					return await item.CompletionSource.Task.ConfigureAwait(false);
				}
			}

			return false;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}

	public Task<bool> PublishAsync(string topic, ReadOnlySequence<byte> data,
		CancellationToken cancellationToken = default)
	{
		var topicBytes = _config.TextEncoding.GetBytes(topic);
		return PublishAsync(topicBytes, data, cancellationToken);
	}

	public bool TryPublish(string topic, ReadOnlySequence<byte> data)
	{
		if (_disposed) return false;

		var topicBytes = _config.TextEncoding.GetBytes(topic);
		var item = new PublishItem(topicBytes, data, CancellationToken.None);

		return _channelWriter.TryWrite(item);
	}

	// Background task that processes the publish queue
	private async Task ProcessPublishQueue(CancellationToken ct)
	{
		var channelReader = _publishChannel.Reader;

		try
		{
			await foreach (var item in channelReader.ReadAllAsync(ct).ConfigureAwait(false))
			{
				try
				{
					if (item.CancellationToken.IsCancellationRequested)
					{
						item.CompletionSource.SetCanceled(item.CancellationToken);
						continue;
					}

					var success = ProcessSinglePublish(item);
					item.CompletionSource.TrySetResult(success);
				}
				catch (OperationCanceledException)
				{
					item.CompletionSource.TrySetCanceled(ct);
				}
				catch (Exception ex)
				{
					item.CompletionSource.TrySetException(ex);
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Expected during disposal
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Publish queue processing error: {ex.Message}");
		}
	}

	// Process single publish item - sends as multi-frame message (topic + data)
	private bool ProcessSinglePublish(PublishItem item)
	{
		try
		{
			var isTopicSend = TrySend(item.Topic, 0, item.Topic.Length, false, item.CancellationToken);
			var isBodySend = TrySend(item.Data, item.CancellationToken);

			item.CancellationToken.ThrowIfCancellationRequested();
			
			return isTopicSend && isBodySend;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return false;
		}
	}

	private bool TrySend(byte[] seq, int offset, int count, bool isLast, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		Msg msg = new();
		msg.InitGC(seq, offset, count);
		var result = _socket.TrySend(ref msg, _config.SendTimeout, !isLast);
		msg.Close();
		return result;
	}

	private bool TrySend(ReadOnlySequence<byte> seq, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var isSend = false;

		foreach (var (segment, isLast) in seq.GetSegments())
		{
			if (TrySend(segment.Array!, segment.Offset, segment.Count, isLast, ct))
			{
				isSend = true;
			}
			else return false;
		}

		return isSend;
	}

	

	public PublisherStats GetStats()
	{
		return new PublisherStats
		{
			QueuedItems = _publishChannel.Reader.CanCount ? _publishChannel.Reader.Count : -1,
			IsCompleted = _publishChannel.Reader.Completion.IsCompleted,
			IsDisposed = _disposed
		};
	}

	// Wait for all queued messages to be sent
	public async Task FlushAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed) return;

		// Mark writer as complete to prevent new items
		_channelWriter.TryComplete();

		// Wait for all items to be processed
		await _publishChannel.Reader.Completion.WaitAsync(cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed) return;
		_disposed = true;

		try
		{
			// Stop accepting new items
			_channelWriter.TryComplete();

			// Cancel background processing
			_disposalCts.CancelAsync();

			// Wait for background task to complete (with timeout)
			using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			try
			{
				await _publishTask.WaitAsync(timeoutCts.Token);
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("Publisher disposal timeout - forcing shutdown");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error during disposal: {ex.Message}");
		}
		finally
		{
			_socket?.Dispose();
			_disposalCts?.Dispose();
		}
	}

	// For synchronous disposal compatibility
	public void Dispose()
	{
		DisposeAsync().AsTask().GetAwaiter().GetResult();
	}
	private readonly record struct PublishItem(byte[] Topic, ReadOnlySequence<byte> Data, CancellationToken CancellationToken)
	{
		public readonly TaskCompletionSource<bool> CompletionSource =
			new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}

// Publisher statistics
public readonly struct PublisherStats
{
	public int QueuedItems { get; init; }
	public bool IsCompleted { get; init; }
	public bool IsDisposed { get; init; }

	public override string ToString()
	{
		return $"QueuedItems: {QueuedItems}, IsCompleted: {IsCompleted}, IsDisposed: {IsDisposed}";
	}
}

//internal sealed class FixedBuffer
//{
//	private static readonly ThreadLocal<byte[]?> Buffer = new();

//	public static bool TryGet(out byte[]? buffer, int size)
//	{
//		buffer = null;

//		if (!Buffer.IsValueCreated) return false;
//		var arr = Buffer.Value;
//		if (arr?.Length >= size)
//		{
//			buffer = arr;
//			return true;
//		}

//		return false;
//	}

//	public static FixedBufferReturn Set(byte[] arr)
//	{
//		return new FixedBufferReturn(arr);
//	}

//	public struct FixedBufferReturn : IDisposable
//	{
//		public FixedBufferReturn(byte[] arr)
//		{
//			Buffer.Value = arr;
//		}

//		public void Dispose()
//		{
//			Buffer.Value = null;
//		}
//	}
//}

public ref struct ReusedBufferSlim
{
	public required int Offset { get; init; }
	public required int Count { get; init; }

}

public class BufferSlimPool : IDisposable
{
	private const int MinBufferSize = 4096;
	private const int MaxBufferSize = MinBufferSize * 20; // under LOH
	private readonly byte[] _buffer;
	
	public BufferSlimPool(int size = MaxBufferSize)
	{
		_buffer = ArrayPool<byte>.Shared.Rent(size);
	}

	public ReusedBufferSlim Take(int size)
	{
		var array = ArrayPool<byte>.Shared.Rent(size);
		return default;
	}

	public void Dispose()
	{
		// TODO release managed resources here
	}
}