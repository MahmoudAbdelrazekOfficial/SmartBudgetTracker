using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Notification;
using Domain.Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface INotificationService
{
    Task<Notification?> CreateBudgetNotificationAsync(Transaction transaction);

    Task<PagedList<NotificationDto>> GetNotificationsAsync(PaginationParams paginationParams);
    Task<Result<bool>> MarkAsReadAsync(int notificationId);
    Task<Result<bool>> MarkAllAsReadAsync();
}
