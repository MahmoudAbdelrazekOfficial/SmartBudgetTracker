using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Notification;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class NotificationsController(INotificationService _notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] PaginationParams paginationParams)
    {
        var result = await _notificationService.GetNotificationsAsync(paginationParams);
        return Ok(result);
    }

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync();
        return StatusCode((int)result.StatusCode, result);
    }
}

