using System.Buffers;
using System.Diagnostics;
using System.Text;
using Fast.Messaging;
using Nerdbank.Streams;

// aliases, since earlier we had IPublication/ISubscription
using IPublisher = Fast.Messaging.IPublication;

const string PipeName = "mux.demo.pipe";
const string Topic = "chat";

IPublisher pub = new Publication(PipeName, Topic);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

_ = pub.RunAsync(cts.Token); // accept subscribers

Console.WriteLine("Server ready. Type messages, '/exit' to quit.");
while (!cts.IsCancellationRequested)
{
    var line = Console.ReadLine();
    if (line is null || line.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;

    byte[] bytes = Encoding.UTF8.GetBytes(line);
    var t = Stopwatch.StartNew();
    await pub.PublishAsync(new ReadOnlySequence<byte>(bytes), cts.Token);
    var t2 = t.ElapsedMilliseconds;
}

cts.Cancel();