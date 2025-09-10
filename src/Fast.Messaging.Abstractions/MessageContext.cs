//namespace Fast.Messaging.Abstractions;

//public readonly struct MessageContext
//{
//    public string Topic { get; }

//    public ReadOnlyMemory<byte> Payload { get; }

//    public string? CorrelationId { get; }

//    public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>? RespondAsync { get; }

//    public MessageContext(string topic, ReadOnlyMemory<byte> payload,
//        string? correlationId = null,
//        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>? respondAsync = null,
//        IReadOnlyDictionary<Type, IMessageFeature>? features = null)
//    {
//        Topic = topic;
//        Payload = payload;
//        CorrelationId = correlationId;
//        RespondAsync = respondAsync;
//        _features = features;
//    }

//}