using Application.Constants;
using Application.DTOs.Dashboard;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class DashboardController(IDashboardService _dashboardService) : ControllerBase
{
    [HttpGet] 
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _dashboardService.GetDashboardSummaryAsync();
        if (result.IsSuccess)
            return Ok(result.Data);

        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet] 
    [ProducesResponseType(typeof(DashboardActivityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivity()
    {
        var result = await _dashboardService.GetDashboardActivityAsync();
        if (result.IsSuccess)
            return Ok(result.Data);

        return StatusCode((int)result.StatusCode, result);
    }
}
