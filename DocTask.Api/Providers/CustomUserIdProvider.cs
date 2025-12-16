using Microsoft.AspNetCore.SignalR;

namespace DocTask.Api.Providers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Lấy userId từ claim "id" trong JWT token
            return connection.User?.FindFirst("id")?.Value;
        }
    }
}
