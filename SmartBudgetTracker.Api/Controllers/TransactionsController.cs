using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Transaction;
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
public class TransactionsController(ITransactionService _transactionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] PaginationParams paginationParams, [FromQuery] TransactionFilterParams filterParams)
    {
        var result = await _transactionService.GetTransactionsAsync(paginationParams, filterParams);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TransactionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(int id)
    {
        var result = await _transactionService.GetTransactionByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransaction([FromBody] UpsertTransactionRequest request)
    {
        var result = await _transactionService.CreateTransactionAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpsertTransactionRequest request)
    {
        var result = await _transactionService.UpdateTransactionAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var result = await _transactionService.DeleteTransactionAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<TransactionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionSummary([FromQuery] ReportDateFilterRequest request)
    {
        var result = await _transactionService.GetTransactionSummaryAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<SpendingByCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpendingByCategory([FromQuery] ReportDateFilterRequest request)
    {
        var result = await _transactionService.GetSpendingByCategoryAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{transactionId}")]
    [ProducesResponseType(typeof(Result<List<TransactionAttachmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttachments(int transactionId)
    {
        var result = await _transactionService.GetAttachmentsAsync(transactionId);
        return StatusCode((int)result.StatusCode, result);
    }


    [HttpPost("{transactionId}")]
    [ProducesResponseType(typeof(Result<List<TransactionAttachmentDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<List<TransactionAttachmentDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAttachments(int transactionId, [FromForm] List<IFormFile> files)
    {
        var result = await _transactionService.UploadAttachmentsAsync(transactionId, files);
        return StatusCode((int)result.StatusCode, result);
    }


    [HttpDelete("{transactionId}/attachments/{attachmentId}")]
    public async Task<IActionResult> Delete(int transactionId, int attachmentId)
    {
        var result = await _transactionService.DeleteAttachmentAsync(transactionId, attachmentId);

        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<TransactionTagDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<List<TransactionTagDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionTags(int transactionId)
    {
        var result = await _transactionService.GetTransactionTagsAsync(transactionId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTagsToTransaction(int transactionId, [FromBody] List<int> tagIds)
    {
        var result = await _transactionService.AddTagsToTransactionAsync(transactionId, tagIds);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTagFromTransaction(int transactionId, int tagId)
    {
        var result = await _transactionService.RemoveTagFromTransactionAsync(transactionId, tagId);
        return StatusCode((int)result.StatusCode, result);
    }

    

}
