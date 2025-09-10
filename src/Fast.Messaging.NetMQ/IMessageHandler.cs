using System.ServiceModel.Channels;

namespace Fast.Messaging.MessagingClient;

public interface IMessageHandler
{
    Task HandleMessageAsync(Message message, CancellationToken cancellationToken = default);
}