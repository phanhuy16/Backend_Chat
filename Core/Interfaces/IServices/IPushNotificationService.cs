using Core.Entities;

namespace Core.Interfaces.IServices
{
    public interface IPushNotificationService
    {
        Task SubscribeAsync(int userId, PushSubscription subscription);
        Task UnsubscribeAsync(int userId, string endpoint);
        Task SendNotificationAsync(int userId, string title, string message, string? url = null);
        Task SendNotificationToAllAsync(string title, string message, string? url = null);
        string GetPublicKey();
    }
}
