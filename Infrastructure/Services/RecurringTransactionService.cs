using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.RecurringTransaction;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class RecurringTransactionService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
    IChangeLogService _changeLogService
    ) : IRecurringTransactionService
{
    public async Task<PagedList<RecurringTransactionDto>> GetRecurringTransactionsAsync(PaginationParams paginationParams)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return new PagedList<RecurringTransactionDto>(new List<RecurringTransactionDto>(), 0, paginationParams.PageNumber, paginationParams.PageSize);

        var query = _unitOfWork.Repository<RecurringTransaction>()
            .GetQueryable()
            .Where(rt => rt.UserId == userId.Value && !rt.IsDeleted);

        var pagedResult = await query
            .OrderBy(rt => rt.NextDueDate)
            .ProjectTo<RecurringTransactionDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(paginationParams.PageNumber, paginationParams.PageSize);

        return pagedResult;
    }

    public async Task<Result<RecurringTransactionDto>> GetRecurringTransactionByIdAsync(int recurringTransactionId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<RecurringTransactionDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var recurringTx = await _unitOfWork.Repository<RecurringTransaction>()
            .GetQueryable()
            .Include(rt => rt.Account)
            .Include(rt => rt.Category)
            .Include(rt => rt.Frequency)
            .FirstOrDefaultAsync(rt => rt.Id == recurringTransactionId && !rt.IsDeleted);

        if (recurringTx is null)
            return Result<RecurringTransactionDto>.Failure("Recurring transaction not found.", HttpStatusCode.NotFound);

        if (recurringTx.UserId != userId.Value)
            return Result<RecurringTransactionDto>.Failure("Recurring transaction not found.", HttpStatusCode.NotFound);

        var recurringTxDto = _mapper.Map<RecurringTransactionDto>(recurringTx);

        return Result<RecurringTransactionDto>.Success(recurringTxDto);
    }

    public async Task<Result<int>> CreateRecurringTransactionAsync(UpsertRecurringTransactionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var account = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.AccountId && a.UserId == userId);
        if (account is null) return Result<int>.Failure("Account not found.", HttpStatusCode.BadRequest);

        var category = await _unitOfWork.Repository<Category>().FindAsync(c => c.Id == request.CategoryId);
        if (category is null) return Result<int>.Failure("Category not found.", HttpStatusCode.BadRequest);

        var frequency = await _unitOfWork.Repository<Frequency>().GetByIdAsync(request.FrequencyId);
        if (frequency is null) return Result<int>.Failure("Frequency not found.", HttpStatusCode.BadRequest);

        var recurringTx = _mapper.Map<RecurringTransaction>(request);
        recurringTx.UserId = userId.Value;

        recurringTx.NextDueDate = request.StartDate;

        _changeLogService.SetCreateChangeLogInfo(recurringTx);
        var createdEntity = await _unitOfWork.Repository<RecurringTransaction>().AddAsync(recurringTx);

        return Result<int>.Success(createdEntity.Id, "Recurring transaction scheduled successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateRecurringTransactionAsync(int recurringTransactionId, UpsertRecurringTransactionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var recurringTxToUpdate = await _unitOfWork.Repository<RecurringTransaction>()
            .FindAsync(rt => rt.Id == recurringTransactionId && rt.UserId == userId.Value && !rt.IsDeleted);

        if (recurringTxToUpdate is null)
            return Result<bool>.Failure("Recurring transaction not found.", HttpStatusCode.NotFound);

        var account = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.AccountId && a.UserId == userId);
        if (account is null) return Result<bool>.Failure("Account not found.", HttpStatusCode.BadRequest);

        var category = await _unitOfWork.Repository<Category>().FindAsync(c => c.Id == request.CategoryId);
        if (category is null) return Result<bool>.Failure("Category not found.", HttpStatusCode.BadRequest);

        var frequency = await _unitOfWork.Repository<Frequency>().GetByIdAsync(request.FrequencyId);
        if (frequency is null) return Result<bool>.Failure("Frequency not found.", HttpStatusCode.BadRequest);

        _mapper.Map(request, recurringTxToUpdate);

        if (recurringTxToUpdate.StartDate != request.StartDate)
        {
            recurringTxToUpdate.NextDueDate = request.StartDate;
        }

        _changeLogService.SetUpdateChangeLogInfo(recurringTxToUpdate);
        await _unitOfWork.Repository<RecurringTransaction>().UpdateAsync(recurringTxToUpdate);

        return Result<bool>.Success(true, "Recurring transaction updated successfully.");
    }

    public async Task<Result<bool>> DeleteRecurringTransactionAsync(int recurringTransactionId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var recurringTxToDelete = await _unitOfWork.Repository<RecurringTransaction>()
            .FindAsync(rt => rt.Id == recurringTransactionId && rt.UserId == userId.Value);

        if (recurringTxToDelete is null || recurringTxToDelete.IsDeleted)
            return Result<bool>.Failure("Recurring transaction not found.", HttpStatusCode.NotFound);

        recurringTxToDelete.IsDeleted = true;
        recurringTxToDelete.IsActive = false; 

        _changeLogService.SetDeleteChangeLogInfo(recurringTxToDelete);
        await _unitOfWork.Repository<RecurringTransaction>().UpdateAsync(recurringTxToDelete);

        return Result<bool>.Success(true, "Recurring transaction deleted successfully.");
    }

    public async Task ProcessDueRecurringTransactionsAsync()
    {
        var today = DateTime.UtcNow.Date;

        var dueTransactions = await _unitOfWork.Repository<RecurringTransaction>()
            .GetQueryable()
            .Include(r => r.Frequency)
            .Where(r => r.IsActive && !r.IsDeleted && r.NextDueDate.Date <= today)
            .ToListAsync();

        foreach (var recurringTx in dueTransactions)
        {
            
            var newTransaction = new Transaction
            {
                Amount = recurringTx.Amount,
                Type = recurringTx.Type,
                Description = recurringTx.Description,
                TransactionDate = recurringTx.NextDueDate,
                UserId = recurringTx.UserId,
                AccountId = recurringTx.AccountId,
                CategoryId = recurringTx.CategoryId,
                RecurringTransactionId = recurringTx.Id
            };
            await _unitOfWork.Repository<Transaction>().AddAsync(newTransaction);

            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(recurringTx.AccountId);
            if (account != null)
            {
                if (newTransaction.Type == TransactionType.Expense)
                    account.CurrentBalance -= newTransaction.Amount;
                else
                    account.CurrentBalance += newTransaction.Amount;

                await _unitOfWork.Repository<Account>().UpdateAsync(account);
            }

            var newNextDueDate = CalculateNextDueDate(recurringTx.NextDueDate, recurringTx.Frequency!.Name);
            recurringTx.NextDueDate = newNextDueDate;

            if (recurringTx.EndDate.HasValue && newNextDueDate.Date > recurringTx.EndDate.Value.Date)
            {
                recurringTx.IsActive = false;
            }

            await _unitOfWork.Repository<RecurringTransaction>().UpdateAsync(recurringTx);
        }
    }

    private DateTime CalculateNextDueDate(DateTime currentDueDate, string frequencyName)
    {
        return frequencyName.ToLower() switch
        {
            "weekly" => currentDueDate.AddDays(7),
            "monthly" => currentDueDate.AddMonths(1),
            "yearly" => currentDueDate.AddYears(1),
            _ => throw new InvalidOperationException("Invalid frequency.")
        };
    }
}

