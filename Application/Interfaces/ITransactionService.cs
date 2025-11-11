using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces;
public interface ITransactionService
{
    Task<PagedList<TransactionDto>> GetTransactionsAsync(PaginationParams paginationParams, TransactionFilterParams filterParams);
    Task<Result<TransactionDto>> GetTransactionByIdAsync(int transactionId);
    Task<Result<int>> CreateTransactionAsync(UpsertTransactionRequest request);
    Task<Result<bool>> UpdateTransactionAsync(int transactionId, UpsertTransactionRequest request);
    Task<Result<bool>> DeleteTransactionAsync(int transactionId);

    Task<Result<TransactionSummaryDto>> GetTransactionSummaryAsync(ReportDateFilterRequest request);
    Task<Result<List<SpendingByCategoryDto>>> GetSpendingByCategoryAsync(ReportDateFilterRequest request);

    Task<Result<List<TransactionAttachmentDto>>> UploadAttachmentsAsync(int transactionId, List<IFormFile> files);
    Task<Result<List<TransactionAttachmentDto>>> GetAttachmentsAsync(int transactionId);
    Task<Result<bool>> DeleteAttachmentAsync(int transactionId, int attachmentId);


    Task<Result<List<TransactionTagDto>>> GetTransactionTagsAsync(int transactionId);
    Task<Result<bool>> AddTagsToTransactionAsync(int transactionId, List<int> tagIds);
    Task<Result<bool>> RemoveTagFromTransactionAsync(int transactionId, int tagId);

}
