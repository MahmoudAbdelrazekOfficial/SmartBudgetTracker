using Application.Common.Pagination;
using Application.Common;
using Application.Constants;
using Application.DTOs.RecurringTransaction;
using Application.Interfaces;
using Domain.Common;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class RecurringTransactionsController(IRecurringTransactionService _recurringTransactionService) : ControllerBase
{
    

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<RecurringTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurringTransactions([FromQuery] PaginationParams paginationParams)
    {
        var result = await _recurringTransactionService.GetRecurringTransactionsAsync(paginationParams);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<RecurringTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RecurringTransactionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecurringTransactionById(int id)
    {
        var result = await _recurringTransactionService.GetRecurringTransactionByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRecurringTransaction([FromBody] UpsertRecurringTransactionRequest request)
    {
        var result = await _recurringTransactionService.CreateRecurringTransactionAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecurringTransaction(int id, [FromBody] UpsertRecurringTransactionRequest request)
    {
        var result = await _recurringTransactionService.UpdateRecurringTransactionAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.User)] 
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TriggerRecurringJobManually()
    {
        BackgroundJob.Enqueue<IRecurringTransactionService>(service => service.ProcessDueRecurringTransactionsAsync());

        return Ok("Background job Worked Succussfully");
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecurringTransaction(int id)
    {
        var result = await _recurringTransactionService.DeleteRecurringTransactionAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
