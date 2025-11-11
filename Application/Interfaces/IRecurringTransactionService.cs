using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.RecurringTransaction;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IRecurringTransactionService
{
    Task<PagedList<RecurringTransactionDto>> GetRecurringTransactionsAsync(PaginationParams paginationParams);
    Task<Result<RecurringTransactionDto>> GetRecurringTransactionByIdAsync(int recurringTransactionId);
    Task<Result<int>> CreateRecurringTransactionAsync(UpsertRecurringTransactionRequest request);
    Task<Result<bool>> UpdateRecurringTransactionAsync(int recurringTransactionId, UpsertRecurringTransactionRequest request);
    Task<Result<bool>> DeleteRecurringTransactionAsync(int recurringTransactionId);
    Task ProcessDueRecurringTransactionsAsync();
}