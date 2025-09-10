//using NetMQ;
//using NetMQ.Sockets;

//public class Subscriber : IAsyncDisposable
//{
//    private readonly SubscriberSocket _socket;
//    private readonly CancellationTokenSource _cancellationTokenSource;
//    private volatile bool _disposed;

//    public Subscriber(string endpoint)
//    {
//        _socket = new SubscriberSocket();
//        _cancellationTokenSource = new CancellationTokenSource();

//        _socket.Connect(endpoint);
//        _socket.Options.ReceiveBuffer = 4 * 1024 * 1024;

//        Console.WriteLine($"Multi-frame subscriber connected to: {endpoint}");
//    }

//    // Subscribe with topic identification
//    public void Subscribe(string topic, Action<string, ReadOnlySpan<byte>> messageHandler)
//    {
//        if (_disposed) return;

//        _socket.Subscribe(topic);

//        Task.Run(async () =>
//        {
//            while (!_cancellationTokenSource.Token.IsCancellationRequested && !_disposed)
//            {
//                try
//                {
//                    _socket.
//					// Receive multi-frame message
//					if (_socket.TryReceiveMultipartMessage(ref NetMQMessage message))
//                    {
                      
//                            if (message.FrameCount >= 2)
//                            {
//                                // Frame 0: Topic
//                                var topicFrame = message[0];
//                                var receivedTopic = topicFrame.ConvertToString();

//                                // Frame 1: Data  
//                                var dataFrame = message[1];
//                                var dataSpan = new ReadOnlySpan<byte>(dataFrame.Buffer, 0, dataFrame.MessageSize);

//                                // Call handler with topic and data
//                                messageHandler(receivedTopic, dataSpan);
//                            }
                        
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Subscriber error: {ex.Message}");
//                    await Task.Delay(100, _cancellationTokenSource.Token);
//                }
//            }
//        }, _cancellationTokenSource.Token);
//    }

//    // Subscribe to all topics (wildcard)
//    public void SubscribeAll(Action<string, ReadOnlySpan<byte>> messageHandler)
//    {
//        Subscribe("", messageHandler); // Empty string subscribes to all
//    }

//    public async ValueTask DisposeAsync()
//    {
//        if (_disposed) return;
//        _disposed = true;

//        _cancellationTokenSource?.Cancel();
//        _socket?.Dispose();
//        _cancellationTokenSource?.Dispose();
//    }
//}