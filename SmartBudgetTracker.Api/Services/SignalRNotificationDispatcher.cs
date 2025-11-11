using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using SmartBudgetTracker.Api.Hubs;

namespace SmartBudgetTracker.Api.Services;

public class SignalRNotificationDispatcher(IHubContext<NotificationHub> _hubContext) : INotificationDispatcher
{
    public async Task SendNotificationAsync(Notification notification)
    {
        await _hubContext.Clients.User(notification.UserId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }
}
