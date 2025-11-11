using Application.Constants;
using Application.DTOs.RecurringTransaction;
using Application.DTOs.Report;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class ReportsController(IReportService _reportService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CashFlowReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashFlowReport([FromQuery] ReportDateFilterRequest request)
    {
        var result = await _reportService.GetCashFlowReportAsync(request);
        if (result.IsSuccess) return Ok(result.Data);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetSpendingByCategoryReport([FromQuery] ReportDateFilterRequest request)
    {
        var result = await _reportService.GetSpendingByCategoryReportAsync(request);
        if (result.IsSuccess) return Ok(result.Data);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SpendingOverTimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpendingOverTimeReport([FromQuery] ReportDateFilterRequest request)
    {
        var result = await _reportService.GetSpendingOverTimeReportAsync(request);
        if (result.IsSuccess) return Ok(result.Data);
        return StatusCode((int)result.StatusCode, result);
    }
}

