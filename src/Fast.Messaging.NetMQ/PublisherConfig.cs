using System.Text;
using NetMQ;

namespace Fast.Messaging;

public class PublisherConfig
{
    public string Endpoint { get; set; } = "tcp://localhost:5556";
    public int ChannelCapacity { get; set; } = 10000;
    public Encoding TextEncoding { get; set;} = Encoding.ASCII; // Default to ASCII for compatibility with ZeroMQ
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan SendTimeout { get; set; } = SendReceiveConstants.InfiniteTimeout;
}