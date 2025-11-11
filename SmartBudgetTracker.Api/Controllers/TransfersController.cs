using Application.Common.Pagination;
using Application.Constants;
using Application.DTOs.Transfere;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class TransfersController(ITransferService _transferService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<TransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfers([FromQuery] GetTransfersRequest request)
    {
        var result = await _transferService.GetTransfersAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<TransferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TransferDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferById(int id)
    {
        var result = await _transferService.GetTransferByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransfer([FromBody] UpsertTransferRequest request)
    {
        var result = await _transferService.CreateTransferAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransfer(int id, [FromBody] UpsertTransferRequest request)
    {
        var result = await _transferService.UpdateTransferAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransfer(int id)
    {
        var result = await _transferService.DeleteTransferAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
}