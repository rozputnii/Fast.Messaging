using NetMQ;
using NetMQ.Sockets;

using (var subSocket = new SubscriberSocket())
{
    subSocket.Options.ReceiveHighWatermark = 1000;
    subSocket.Connect("tcp://localhost:12345");
    Span<byte> topic = stackalloc byte[20];
	subSocket.SubscribeToAnyTopic();

	Console.WriteLine("Subscriber socket connecting...");
    while (true)
    {
        string messageTopicReceived = subSocket.ReceiveFrameString();
        string messageReceived = subSocket.ReceiveFrameString();
        Console.WriteLine(messageReceived);
    }
}