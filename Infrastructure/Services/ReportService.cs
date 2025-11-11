using Application.DTOs.RecurringTransaction;
using Application.DTOs.Report;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Infrastructure.Services;
public class ReportService(
    IUnitOfWork _unitOfWork,
    ICurrentUserService _currentUserService
    ) : IReportService
{

    public async Task<Result<List<CashFlowReportDto>>> GetCashFlowReportAsync(ReportDateFilterRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null) return Result<List<CashFlowReportDto>>.Failure("User not authenticated.");

        var intermediateResult = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted &&
                         t.TransactionDate.Date >= request.StartDate.Date &&
                         t.TransactionDate.Date <= request.EndDate.Date)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalIncome = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : 0),
                TotalExpense = g.Sum(t => t.Type == TransactionType.Expense ? t.Amount : 0)
            })
            .ToListAsync(); 

        var finalReport = intermediateResult
            .Select(r => new CashFlowReportDto
            {
                Period = new DateTime(r.Year, r.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                TotalIncome = r.TotalIncome,
                TotalExpense = r.TotalExpense
            })
            .OrderBy(r => DateTime.Parse(r.Period)) 
            .ToList();

        return Result<List<CashFlowReportDto>>.Success(finalReport);
    }
    public async Task<Result<List<SpendingByCategoryDto>>> GetSpendingByCategoryReportAsync(ReportDateFilterRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null) return Result<List<SpendingByCategoryDto>>.Failure("User not authenticated.");

        var query = _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted &&
                         t.Type == TransactionType.Expense &&
                         t.TransactionDate.Date >= request.StartDate.Date &&
                         t.TransactionDate.Date <= request.EndDate.Date);

        var totalSpending = await query.SumAsync(t => t.Amount);
        if (totalSpending == 0) return Result<List<SpendingByCategoryDto>>.Success(new List<SpendingByCategoryDto>());

        var spendingByCategory = await query
            .Include(t => t.Category)
            .GroupBy(t => t.Category.Name)
            .Select(g => new SpendingByCategoryDto
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                Percentage = Math.Round((double)(g.Sum(t => t.Amount) / totalSpending) * 100, 2)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToListAsync();

        return Result<List<SpendingByCategoryDto>>.Success(spendingByCategory);
    }

    public async Task<Result<List<SpendingOverTimeDto>>> GetSpendingOverTimeReportAsync(ReportDateFilterRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null) return Result<List<SpendingOverTimeDto>>.Failure("User not authenticated.");

        var intermediateResult = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted &&
                         t.Type == TransactionType.Expense &&
                         t.TransactionDate.Date >= request.StartDate.Date &&
                         t.TransactionDate.Date <= request.EndDate.Date)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalSpending = g.Sum(t => t.Amount)
            })
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToListAsync(); 

        var finalReport = intermediateResult
            .Select(r => new SpendingOverTimeDto
            {
                Period = new DateTime(r.Year, r.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                TotalSpending = r.TotalSpending
            })
            .ToList();

        return Result<List<SpendingOverTimeDto>>.Success(finalReport);
    }
}

