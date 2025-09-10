namespace Fast.Messaging;

public sealed class SubscribeAttribute : Attribute
{
    public string Topic { get; }
    public SubscribeAttribute(string topic)
    {
        Topic = topic;
	}
}

//public sealed class Subscriber
//{
//    public void Do()
//    {
//        ReadOnlySpan<byte> src = "ok"u8;
//	}
//}