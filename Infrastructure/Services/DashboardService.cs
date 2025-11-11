using Application.DTOs.Dashboard;
using Application.DTOs.Transaction;
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
public class DashboardService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    IChangeLogService _changeLogService,
    ICurrentUserService _currentUserService
    ) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<DashboardSummaryDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var summaryDto = new DashboardSummaryDto();
        var now = DateTime.UtcNow;

        summaryDto.TotalBalance = await _unitOfWork.Repository<Account>()
            .GetQueryable()
            .Where(a => a.UserId == userId.Value && !a.IsDeleted && a.IsActive)
            .SumAsync(a => a.CurrentBalance);

        var budgets = await _unitOfWork.Repository<Budget>()
            .GetQueryable()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId.Value && b.Year == now.Year && b.Month == now.Month && !b.IsDeleted)
            .ToListAsync();

        foreach (var budget in budgets)
        {
            var spending = await _unitOfWork.Repository<Transaction>()
                .GetQueryable()
                .Where(t => t.UserId == userId.Value && t.CategoryId == budget.CategoryId &&
                             t.TransactionDate.Year == now.Year && t.TransactionDate.Month == now.Month &&
                             t.Type == TransactionType.Expense && !t.IsDeleted)
                .SumAsync(t => t.Amount);

            summaryDto.BudgetSummaries.Add(new BudgetSummaryDto
            {
                CategoryName = budget.Category?.Name ?? "Unknown",
                BudgetedAmount = budget.Amount,
                ActualSpending = spending
            });
        }

        return Result<DashboardSummaryDto>.Success(summaryDto);
    }

    public async Task<Result<DashboardActivityDto>> GetDashboardActivityAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<DashboardActivityDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var activityDto = new DashboardActivityDto();
        var now = DateTime.UtcNow;

        activityDto.RecentTransactions = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted)
            .OrderByDescending(t => t.TransactionDate)
            .Take(5)
            .ProjectTo<TransactionDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        activityDto.UpcomingBills = await _unitOfWork.Repository<RecurringTransaction>()
            .GetQueryable()
            .Where(rt => rt.UserId == userId.Value && rt.IsActive && !rt.IsDeleted && rt.Type == TransactionType.Expense &&
                         rt.NextDueDate >= now.Date && rt.NextDueDate <= now.Date.AddDays(7))
            .OrderBy(rt => rt.NextDueDate)
            .Select(rt => new UpcomingBillDto
            {
                Id = rt.Id,
                Description = rt.Description ?? "Recurring Bill",
                Amount = rt.Amount,
                DueDate = rt.NextDueDate
            })
            .ToListAsync();

        return Result<DashboardActivityDto>.Success(activityDto);
    }
}