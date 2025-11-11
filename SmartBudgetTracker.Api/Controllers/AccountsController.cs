using Application.Common;
using Application.Common.Pagination;
using Application.Constants;
using Application.DTOs.Account;
using Application.Interfaces;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
public class AccountsController(IAccountService _accountService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(PagedList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts([FromQuery] PaginationParams @params)
    {
        var pagedResult = await _accountService.GetAccountsAsync(@params);
        return Ok(pagedResult);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AccountDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById(int id)
    {
        var result = await _accountService.GetAccountByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _accountService.CreateAccountAsync(request);

        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
    {
        var result = await _accountService.UpdateAccountAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleAccountStatus(int id)
    {
        var result = await _accountService.ToggleAccountStatusAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.User)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var result = await _accountService.SoftDeleteAccountAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
