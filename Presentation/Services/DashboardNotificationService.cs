using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;

namespace Presentation.Services
{
    public interface IDashboardNotificationService
    {
        Task NotifyDashboardUpdate(string eventType, string entityType, string message);
    }

    public class DashboardNotificationService : IDashboardNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public DashboardNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyDashboardUpdate(string eventType, string entityType, string message)
        {
            await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
            {
                eventType = eventType, // "create", "update", "delete"
                entityType = entityType, // "article", "account", "category", "comment"
                message = message,
                timestamp = DateTime.Now
            });
        }
    }
}
