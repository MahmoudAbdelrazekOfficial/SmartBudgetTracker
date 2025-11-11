using Application.DTOs.RecurringTransaction;
using Application.DTOs.Report;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IReportService
{
    Task<Result<List<CashFlowReportDto>>> GetCashFlowReportAsync(ReportDateFilterRequest request);
    Task<Result<List<SpendingByCategoryDto>>> GetSpendingByCategoryReportAsync(ReportDateFilterRequest request);
    Task<Result<List<SpendingOverTimeDto>>> GetSpendingOverTimeReportAsync(ReportDateFilterRequest request);
}