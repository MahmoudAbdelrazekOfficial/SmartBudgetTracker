using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Account;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class AccountService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
     IChangeLogService _changeLogService
    ) :IAccountService
{
    public async Task<PagedList<AccountDto>> GetAccountsAsync(PaginationParams paginationParams)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return new PagedList<AccountDto>(
                new List<AccountDto>(), 0, paginationParams.PageNumber, paginationParams.PageSize);
        }

        var query = _unitOfWork.Repository<Account>()
            .GetQueryable()
            .Where(a => a.UserId == userId.Value && a.IsActive) 
            .OrderBy(a => a.Name);

        var pagedResult = await query
            .ProjectTo<AccountDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(paginationParams.PageNumber, paginationParams.PageSize);

        return pagedResult;
    }

    public async Task<Result<AccountDto>> GetAccountByIdAsync(int accountId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result<AccountDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);
        }

        var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);

        if (account is null || account.IsDeleted)
        {
            return Result<AccountDto>.Failure("Account not found.", HttpStatusCode.NotFound);
        }
        if (account.UserId != userId.Value)
        {         
            return Result<AccountDto>.Failure("Account not found.", HttpStatusCode.NotFound);
        }
        var accountDto = _mapper.Map<AccountDto>(account);

        return Result<AccountDto>.Success(accountDto);
    }

    public async Task<Result<int>> CreateAccountAsync(CreateAccountRequest request)
    {       
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var existingAccounts = await _unitOfWork.Repository<Account>().GetAsync(
            a => a.UserId == userId.Value && a.Name.ToLower() == request.Name.ToLower() && a.IsDeleted == false
        );

        if (existingAccounts.Any())
        {
            return Result<int>.Failure($"An account with the name '{request.Name}' already exists.", HttpStatusCode.BadRequest);
        }

        var account = _mapper.Map<Account>(request);

        account.UserId = userId.Value;

        _changeLogService.SetCreateChangeLogInfo(account);

        var createdAccount = await _unitOfWork.Repository<Account>().AddAsync(account);

        return Result<int>.Success(createdAccount.Id, "Account created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateAccountAsync(int accountId, UpdateAccountRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var accountToUpdate = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);
        if (accountToUpdate is null || accountToUpdate.IsDeleted)
            return Result<bool>.Failure("Account not found.", HttpStatusCode.NotFound);

        if (!accountToUpdate.IsActive)
        {
            return Result<bool>.Failure("Cannot update a deactivated account. Please activate it first.", HttpStatusCode.BadRequest);
        }

        if (accountToUpdate.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to update this account.", HttpStatusCode.Forbidden);

        var existingAccounts = await _unitOfWork.Repository<Account>().GetAsync(
            a => a.UserId == userId.Value &&
                 a.Name.ToLower() == request.Name.ToLower() &&
                 a.Id != accountId 
        );

        if (existingAccounts.Any())
            return Result<bool>.Failure($"An account with the name '{request.Name}' already exists.", HttpStatusCode.BadRequest);

        _mapper.Map(request, accountToUpdate);
        _changeLogService.SetUpdateChangeLogInfo(accountToUpdate);

        await _unitOfWork.Repository<Account>().UpdateAsync(accountToUpdate);

        return Result<bool>.Success(true, "Account updated successfully.", HttpStatusCode.OK);
    }

    public async Task<Result<bool>> ToggleAccountStatusAsync(int accountId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var account = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);

        if (account is null)
            return Result<bool>.Failure("Account not found.", HttpStatusCode.NotFound);

        if (account.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to modify this account.", HttpStatusCode.Forbidden);

        account.IsActive = !account.IsActive;

        _changeLogService.SetUpdateChangeLogInfo(account);
        await _unitOfWork.Repository<Account>().UpdateAsync(account);

        var message = account.IsActive ? "Account has been activated." : "Account has been deactivated.";
        return Result<bool>.Success(account.IsActive, message, HttpStatusCode.OK);
    }

    public async Task<Result<bool>> SoftDeleteAccountAsync(int accountId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var accountToDelete = await _unitOfWork.Repository<Account>().GetByIdAsync(accountId);

        if (accountToDelete is null || accountToDelete.IsDeleted)
            return Result<bool>.Failure("Account not found.", HttpStatusCode.NotFound);

        if (accountToDelete.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to delete this account.", HttpStatusCode.Forbidden);

        accountToDelete.IsDeleted = true;
        _changeLogService.SetDeleteChangeLogInfo(accountToDelete);

        await _unitOfWork.Repository<Account>().UpdateAsync(accountToDelete);

        return Result<bool>.Success(true, "Account deleted successfully.", HttpStatusCode.OK);
    }
}

