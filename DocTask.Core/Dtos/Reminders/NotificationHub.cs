using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DocTask.Api.Providers
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // Lấy từ JWT token
            if (!string.IsNullOrEmpty(userId))
            {
                // Thêm user vào group theo userId
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                _logger.LogInformation($"User {userId} connected with ConnectionId: {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
                _logger.LogInformation($"User {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
