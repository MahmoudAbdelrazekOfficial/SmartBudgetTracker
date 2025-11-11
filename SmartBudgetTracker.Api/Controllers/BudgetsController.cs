using Application.Constants;
using Application.DTOs.Budget;
using Application.Interfaces;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class BudgetsController(IBudgetService _budgetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Result<List<BudgetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgets([FromQuery] GetBudgetsRequest request)
    {
        var result = await _budgetService.GetBudgetsAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BudgetDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBudgetById(int id)
    {
        var result = await _budgetService.GetBudgetByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status409Conflict)] 
    public async Task<IActionResult> CreateBudget([FromBody] UpsertBudgetRequest request)
    {
        var result = await _budgetService.CreateBudgetAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBudget(int id, [FromBody] UpsertBudgetRequest request)
    {
        var result = await _budgetService.UpdateBudgetAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBudget(int id)
    {
        var result = await _budgetService.DeleteBudgetAsync(id);

        if (result.StatusCode == HttpStatusCode.NoContent || (result.IsSuccess && result.Data))
        {
            return NoContent();
        }

        return StatusCode((int)result.StatusCode, result);
    }
    [HttpGet]
    [ProducesResponseType(typeof(Result<List<BudgetProgressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetProgress([FromQuery] GetBudgetsRequest request)
    {
        var result = await _budgetService.GetBudgetProgressAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<BudgetSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetSuggestions([FromQuery] GetBudgetsRequest request)
    {
        var result = await _budgetService.GetBudgetSuggestionsAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

}
