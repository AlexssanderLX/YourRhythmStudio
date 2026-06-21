using Foundation.Access.Models;

namespace Foundation.Access.Abstractions;

public interface IAccessMessageSender
{
    Task SendAsync(AccessNotificationMessage message, CancellationToken cancellationToken = default);
}
