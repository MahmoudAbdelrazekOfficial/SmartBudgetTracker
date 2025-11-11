using Application.DTOs.Budget;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class BudgetService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
    IChangeLogService _changeLogService
    ) : IBudgetService
{

    public async Task<Result<List<BudgetDto>>> GetBudgetsAsync(GetBudgetsRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<BudgetDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var budgets = await _unitOfWork.Repository<Budget>()
            .GetQueryable()
            .AsNoTracking() 
            .Where(b => b.UserId == userId.Value &&
                        b.Month == request.Month &&
                        b.Year == request.Year &&
                        !b.IsDeleted)
            .ProjectTo<BudgetDto>(_mapper.ConfigurationProvider) 
            .ToListAsync();

        return Result<List<BudgetDto>>.Success(budgets);
    }

    public async Task<Result<BudgetDto>> GetBudgetByIdAsync(int budgetId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<BudgetDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var budget = await _unitOfWork.Repository<Budget>()
            .GetQueryable()
            .AsNoTracking()
            .Where(b => b.Id == budgetId && b.UserId == userId.Value && !b.IsDeleted)
            .ProjectTo<BudgetDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (budget is null)
            return Result<BudgetDto>.Failure("Budget not found.", HttpStatusCode.NotFound);

        return Result<BudgetDto>.Success(budget);
    }

    public async Task<Result<List<BudgetProgressDto>>> GetBudgetProgressAsync(GetBudgetsRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<BudgetProgressDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);
        var budgets = await _unitOfWork.Repository<Budget>()
            .GetQueryable()
            .Include(b => b.Category) 
            .Where(b => b.UserId == userId.Value && b.Month == request.Month && b.Year == request.Year && !b.IsDeleted)
            .ToListAsync();

        var allSpending = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && t.Type == Domain.Enums.TransactionType.Expense &&
                        t.TransactionDate.Month == request.Month && t.TransactionDate.Year == request.Year && !t.IsDeleted)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, TotalAmount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.TotalAmount);

        var progressList = new List<BudgetProgressDto>();
        foreach (var budget in budgets)
        {
            allSpending.TryGetValue(budget.CategoryId, out var actualSpending);

            var remaining = budget.Amount - actualSpending;
            var progressPercentage = budget.Amount > 0 ? (double)(actualSpending / budget.Amount) * 100 : 0;

            progressList.Add(new BudgetProgressDto
            {
                BudgetId = budget.Id,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? "Unknown",
                BudgetedAmount = budget.Amount,
                ActualSpending = actualSpending,
                RemainingAmount = remaining,
                IsOverBudget = actualSpending > budget.Amount,
                ProgressPercentage = Math.Min(progressPercentage, 100) 
            });
        }

        return Result<List<BudgetProgressDto>>.Success(progressList);
    }

    public async Task<Result<List<BudgetSuggestionDto>>> GetBudgetSuggestionsAsync(GetBudgetsRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<BudgetSuggestionDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var budgetedCategoryIds = await _unitOfWork.Repository<Budget>()
            .GetQueryable()
            .Where(b => b.UserId == userId.Value && b.Month == request.Month && b.Year == request.Year && !b.IsDeleted)
            .Select(b => b.CategoryId)
            .ToHashSetAsync();

        var suggestions = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId.Value &&
                        t.Type == Domain.Enums.TransactionType.Expense &&
                        t.TransactionDate.Month == request.Month &&
                        t.TransactionDate.Year == request.Year &&
                        !t.IsDeleted &&
                        !budgetedCategoryIds.Contains(t.CategoryId)) 
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new BudgetSuggestionDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalSpent = g.Sum(t => t.Amount)
            })
            .Where(s => s.TotalSpent > 0) 
            .OrderByDescending(s => s.TotalSpent)
            .ToListAsync();

        return Result<List<BudgetSuggestionDto>>.Success(suggestions);
    }

    public async Task<Result<int>> CreateBudgetAsync(UpsertBudgetRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var category = await _unitOfWork.Repository<Category>()
            .FindAsync(c => c.Id == request.CategoryId && (!c.UserId.HasValue || c.UserId == userId.Value) && !c.IsDeleted);

        if (category is null)
            return Result<int>.Failure("Category not found.", HttpStatusCode.BadRequest);

        var existingBudget = await _unitOfWork.Repository<Budget>()
            .FindAsync(b => b.UserId == userId.Value &&
                           b.CategoryId == request.CategoryId &&
                           b.Month == request.Month &&
                           b.Year == request.Year &&
                           !b.IsDeleted);

        if (existingBudget is not null)
            return Result<int>.Failure("A budget for this category already exists for the specified month and year.", HttpStatusCode.Conflict); // Conflict (409) is more suitable here

        var budget = _mapper.Map<Budget>(request);
        budget.UserId = userId.Value;

        _changeLogService.SetCreateChangeLogInfo(budget);

        var createdBudget = await _unitOfWork.Repository<Budget>().AddAsync(budget);

        return Result<int>.Success(createdBudget.Id, "Budget created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateBudgetAsync(int budgetId, UpsertBudgetRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var budgetToUpdate = await _unitOfWork.Repository<Budget>()
            .FindAsync(b => b.Id == budgetId && b.UserId == userId.Value && !b.IsDeleted);

        if (budgetToUpdate is null)
            return Result<bool>.Failure("Budget not found.", HttpStatusCode.NotFound);

        var existingBudget = await _unitOfWork.Repository<Budget>()
            .FindAsync(b => b.UserId == userId.Value &&
                            b.CategoryId == request.CategoryId &&
                            b.Month == request.Month &&
                            b.Year == request.Year &&
                            b.Id != budgetId && // Exclude the current budget from the check
                            !b.IsDeleted);

        if (existingBudget is not null)
            return Result<bool>.Failure("Another budget for this category already exists for the specified month and year.", HttpStatusCode.Conflict);

        _mapper.Map(request, budgetToUpdate);

        _changeLogService.SetUpdateChangeLogInfo(budgetToUpdate);
        await _unitOfWork.Repository<Budget>().UpdateAsync(budgetToUpdate);

        return Result<bool>.Success(true, "Budget updated successfully.");
    }

    public async Task<Result<bool>> DeleteBudgetAsync(int budgetId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var budgetToDelete = await _unitOfWork.Repository<Budget>()
            .FindAsync(b => b.Id == budgetId && b.UserId == userId.Value && !b.IsDeleted);

        if (budgetToDelete is null)
            return Result<bool>.Failure("Budget not found.", HttpStatusCode.NotFound);

        budgetToDelete.IsDeleted = true;

        _changeLogService.SetDeleteChangeLogInfo(budgetToDelete);

        await _unitOfWork.Repository<Budget>().UpdateAsync(budgetToDelete);

        return Result<bool>.Success(true, "Budget deleted successfully.");
    }
}
