using Application.Common.Pagination;
using Application.Common;
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;
public class TransactionService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
    IChangeLogService _changeLogService,
    IWebHostEnvironment _webHostEnvironment,
    IHttpContextAccessor _httpContextAccessor,
    IMemoryCache _cache,
    INotificationService _notificationService,
   INotificationDispatcher _notificationDispatcher
    ) : ITransactionService
{
    public async Task<PagedList<TransactionDto>> GetTransactionsAsync(PaginationParams paginationParams, TransactionFilterParams filterParams)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return new PagedList<TransactionDto>([], 0, paginationParams.PageNumber, paginationParams.PageSize);

        // create cash key
        string cacheKey = $"Transactions_User_{userId}_Page_{paginationParams.PageNumber}_Size_{paginationParams.PageSize}" +
                          $"_Account_{filterParams.AccountId}_Type_{filterParams.Type}" +
                          $"_Start_{filterParams.StartDate:yyyy-MM-dd}_End_{filterParams.EndDate:yyyy-MM-dd}";

        if (_cache.TryGetValue(cacheKey, out PagedList<TransactionDto> cachedResult))
        {
            return cachedResult; 
        }

        var query = _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value);

        // Apply filters
        if (filterParams.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filterParams.AccountId.Value);

        if (filterParams.Type.HasValue)
            query = query.Where(t => t.Type == filterParams.Type.Value);

        if (filterParams.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate.Date >= filterParams.StartDate.Value.Date);

        if (filterParams.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate.Date <= filterParams.EndDate.Value.Date);

        var pagedResult = await query
            .OrderByDescending(t => t.TransactionDate)
            .ProjectTo<TransactionDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(paginationParams.PageNumber, paginationParams.PageSize);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(60)); 

        _cache.Set(cacheKey, pagedResult, cacheOptions);

        return pagedResult;
    }

    public async Task<Result<TransactionDto>> GetTransactionByIdAsync(int transactionId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<TransactionDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transaction = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Where(t => t.Id == transactionId && t.UserId == userId.Value)
            .ProjectTo<TransactionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (transaction is null)
            return Result<TransactionDto>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        return Result<TransactionDto>.Success(transaction);
    }

    public async Task<Result<int>> CreateTransactionAsync(UpsertTransactionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var account = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.AccountId && a.UserId == userId.Value && !a.IsDeleted);
        if (account is null)
            return Result<int>.Failure("Account not found.", HttpStatusCode.BadRequest);

        if (request.Splits.Any())
        {
            if (request.Splits.Sum(s => s.Amount) != request.Amount)
                return Result<int>.Failure("The sum of split amounts must equal the total transaction amount.", HttpStatusCode.BadRequest);

            var splitCategoryIds = request.Splits.Select(s => s.CategoryId).Distinct().ToList();
            var validCategoriesCount = await _unitOfWork.Repository<Category>()
                .GetQueryable()
                .CountAsync(c => splitCategoryIds.Contains(c.Id) && (!c.UserId.HasValue || c.UserId == userId.Value));

            if (validCategoriesCount != splitCategoryIds.Count)
                return Result<int>.Failure("One or more split categories are invalid.", HttpStatusCode.BadRequest);

            request.CategoryId = request.Splits.First().CategoryId;
        }
        else if (!request.CategoryId.HasValue)
        {
            return Result<int>.Failure("Category is required for non-split transactions.", HttpStatusCode.BadRequest);
        }

        var transaction = _mapper.Map<Transaction>(request);
        transaction.UserId = userId.Value;
        transaction.IsSplit = request.Splits.Any();

        if (request.TagIds.Any())
        {
            foreach (var tagId in request.TagIds.Distinct())
            {
                transaction.TransactionTags.Add(new TransactionTag { TagId = tagId });
            }
        }

        _changeLogService.SetCreateChangeLogInfo(transaction);

        if (transaction.Type == TransactionType.Expense)
            account.CurrentBalance -= transaction.Amount;
        else
            account.CurrentBalance += transaction.Amount;

        await _unitOfWork.Repository<Account>().UpdateAsync(account);
        var createdTransaction = await _unitOfWork.Repository<Transaction>().AddAsync(transaction);

        //send Notification
        var transactionWithDetails = await _unitOfWork.Repository<Transaction>()
        .GetQueryable()
        .Include(t => t.Category)
        .FirstOrDefaultAsync(t => t.Id == createdTransaction.Id);

        if (transactionWithDetails != null)
        {
            var notificationToSend = await _notificationService.CreateBudgetNotificationAsync(transactionWithDetails);
            if (notificationToSend != null)
            {
                await _notificationDispatcher.SendNotificationAsync(notificationToSend);
            }
        }

        return Result<int>.Success(createdTransaction.Id, "Transaction created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateTransactionAsync(int transactionId, UpsertTransactionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transactionToUpdate = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.Account)
            .Include(t => t.Splits)
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value);

        if (transactionToUpdate is null)
            return Result<bool>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var originalAccount = transactionToUpdate.Account!;
        if (transactionToUpdate.Type == TransactionType.Expense)
            originalAccount.CurrentBalance += transactionToUpdate.Amount;
        else
            originalAccount.CurrentBalance -= transactionToUpdate.Amount;

        _mapper.Map(request, transactionToUpdate);
        transactionToUpdate.IsSplit = request.Splits.Any();
        if (transactionToUpdate.IsSplit)
        {
            if (request.Splits.Sum(s => s.Amount) != request.Amount)
                return Result<bool>.Failure("The sum of split amounts must equal the total transaction amount.", HttpStatusCode.BadRequest);
          
        }
        else if (transactionToUpdate.CategoryId == 0)
        {
            return Result<bool>.Failure("Category is required for non-split transactions.", HttpStatusCode.BadRequest);
        }

        transactionToUpdate.Splits.Clear();
        transactionToUpdate.TransactionTags.Clear();

        foreach (var splitRequest in request.Splits)
            transactionToUpdate.Splits.Add(_mapper.Map<TransactionSplit>(splitRequest));

        foreach (var tagId in request.TagIds.Distinct())
            transactionToUpdate.TransactionTags.Add(new TransactionTag { TagId = tagId, TransactionId = transactionId });

        Account accountToUpdate;
        if (transactionToUpdate.AccountId == originalAccount.Id)
        {
            accountToUpdate = originalAccount;
        }
        else
        {
            accountToUpdate = await _unitOfWork.Repository<Account>().GetByIdAsync(transactionToUpdate.AccountId);
            if (accountToUpdate is null || accountToUpdate.UserId != userId.Value)
                return Result<bool>.Failure("New account not found.", HttpStatusCode.BadRequest);
            await _unitOfWork.Repository<Account>().UpdateAsync(originalAccount);
        }

        if (transactionToUpdate.Type == TransactionType.Expense)
            accountToUpdate.CurrentBalance -= transactionToUpdate.Amount;
        else
            accountToUpdate.CurrentBalance += transactionToUpdate.Amount;

        _changeLogService.SetUpdateChangeLogInfo(transactionToUpdate);

        await _unitOfWork.Repository<Transaction>().UpdateAsync(transactionToUpdate);
        await _unitOfWork.Repository<Account>().UpdateAsync(accountToUpdate);

        return Result<bool>.Success(true, "Transaction updated successfully.");
    }

    public async Task<Result<bool>> DeleteTransactionAsync(int transactionId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transactionToDelete = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value);

        if (transactionToDelete is null || transactionToDelete.IsDeleted)
            return Result<bool>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var account = transactionToDelete.Account!;

        if (transactionToDelete.Type == TransactionType.Expense)
            account.CurrentBalance += transactionToDelete.Amount;
        else 
            account.CurrentBalance -= transactionToDelete.Amount;

        await _unitOfWork.Repository<Account>().UpdateAsync(account);

        transactionToDelete.IsDeleted = true;
        _changeLogService.SetDeleteChangeLogInfo(transactionToDelete);
        await _unitOfWork.Repository<Transaction>().UpdateAsync(transactionToDelete);

        string cacheKey = $"Transaction_{transactionId}_User_{userId}";
        _cache.Remove(cacheKey);

        return Result<bool>.Success(true, "Transaction deleted successfully.");
    }

    public async Task<Result<List<TransactionAttachmentDto>>> UploadAttachmentsAsync(int transactionId, List<IFormFile> files)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<TransactionAttachmentDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transaction = await _unitOfWork.Repository<Transaction>().FindAsync(t => t.Id == transactionId && t.UserId == userId && !t.IsDeleted);
        if (transaction is null)
            return Result<List<TransactionAttachmentDto>>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var storagePath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments");
        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        var createdAttachments = new List<TransactionAttachment>();

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var fullPath = Path.Combine(storagePath, storedFileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new TransactionAttachment
                {
                    TransactionId = transactionId,
                    FileName = file.FileName, 
                    FilePath = storedFileName, 
                    UploadedAt = DateTime.UtcNow
                };

                var created = await _unitOfWork.Repository<TransactionAttachment>().AddAsync(attachment);
                createdAttachments.Add(created);
            }
        }

        var dtos = createdAttachments.Select(MapToDtoWithUrl).ToList();
        return Result<List<TransactionAttachmentDto>>.Success(dtos, "Attachments uploaded successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<TransactionSummaryDto>> GetTransactionSummaryAsync(ReportDateFilterRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<TransactionSummaryDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transactionsInDateRange = _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .AsNoTracking()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted &&
                          t.TransactionDate >= request.StartDate &&
                          t.TransactionDate <= request.EndDate);

        var totalIncome = await transactionsInDateRange
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => t.Amount);

        var totalExpense = await transactionsInDateRange
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => t.Amount);

        var summaryDto = new TransactionSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetResult = totalIncome - totalExpense,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        return Result<TransactionSummaryDto>.Success(summaryDto, "Summary retrieved successfully.");
    }

    public async Task<Result<List<SpendingByCategoryDto>>> GetSpendingByCategoryAsync(ReportDateFilterRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<SpendingByCategoryDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var spendingByCategory = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId.Value && !t.IsDeleted &&
                          t.Type == TransactionType.Expense &&
                          t.TransactionDate >= request.StartDate &&
                          t.TransactionDate <= request.EndDate)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(group => new SpendingByCategoryDto
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.Name,
                TotalAmount = group.Sum(t => t.Amount)
            })
            .OrderByDescending(dto => dto.TotalAmount)
            .ToListAsync();

        return Result<List<SpendingByCategoryDto>>.Success(spendingByCategory, "Spending by category retrieved successfully.");
    }

    public async Task<Result<List<TransactionAttachmentDto>>> GetAttachmentsAsync(int transactionId)
    {
        var userId = _currentUserService.UserId;
        var attachments = await _unitOfWork.Repository<TransactionAttachment>()
            .GetAsync(a => a.TransactionId == transactionId && a.Transaction!.UserId == userId); 

        var dtos = attachments.Select(MapToDtoWithUrl).ToList();
        return Result<List<TransactionAttachmentDto>>.Success(dtos);
    }

    public async Task<Result<bool>> DeleteAttachmentAsync(int transactionId, int attachmentId)
    {
        var userId = _currentUserService.UserId;
        var attachment = await _unitOfWork.Repository<TransactionAttachment>()
            .FindAsync(a => a.Id == attachmentId && a.TransactionId == transactionId && a.Transaction!.UserId == userId);

        if (attachment is null)
            return Result<bool>.Failure("Attachment not found.", HttpStatusCode.NotFound);

        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", attachment.FilePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await _unitOfWork.Repository<TransactionAttachment>().DeleteAsync(attachment);

        return Result<bool>.Success(true, "Attachment deleted successfully.", HttpStatusCode.OK);
    }

    public async Task<Result<bool>> AddTagsToTransactionAsync(int transactionId, List<int> tagIds)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        if (tagIds == null || !tagIds.Any())
            return Result<bool>.Failure("No tags provided.", HttpStatusCode.BadRequest);

        var transaction = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value && !t.IsDeleted);

        if (transaction is null)
            return Result<bool>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var existingTagIds = transaction.TransactionTags.Select(tt => tt.TagId).ToHashSet();
        var newTagIds = tagIds.Distinct().Where(id => !existingTagIds.Contains(id)).ToList();

        if (!newTagIds.Any())
            return Result<bool>.Failure("All tags already exist for this transaction.", HttpStatusCode.BadRequest);

        foreach (var tagId in newTagIds)
        {
            transaction.TransactionTags.Add(new TransactionTag
            {
                TransactionId = transactionId,
                TagId = tagId
            });
        }

        await _unitOfWork.Repository<Transaction>().UpdateAsync(transaction);

        return Result<bool>.Success(true, "Tags added successfully.");
    }

    public async Task<Result<bool>> RemoveTagFromTransactionAsync(int transactionId, int tagId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transaction = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value && !t.IsDeleted);

        if (transaction is null)
            return Result<bool>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var tagToRemove = transaction.TransactionTags.FirstOrDefault(tt => tt.TagId == tagId);
        if (tagToRemove is null)
            return Result<bool>.Failure("Tag not associated with this transaction.", HttpStatusCode.BadRequest);

        transaction.TransactionTags.Remove(tagToRemove);

        await _unitOfWork.Repository<Transaction>().UpdateAsync(transaction);

        return Result<bool>.Success(true, "Tag removed successfully.");
    }

    public async Task<Result<List<TransactionTagDto>>> GetTransactionTagsAsync(int transactionId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<TransactionTagDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transaction = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .Include(t => t.TransactionTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value && !t.IsDeleted);

        if (transaction is null)
            return Result<List<TransactionTagDto>>.Failure("Transaction not found.", HttpStatusCode.NotFound);

        var tags = transaction.TransactionTags
            .Where(tt => !tt.Tag!.IsDeleted)
            .Select(tt => new TransactionTagDto
            {
                TagId = tt.TagId,
                Name = tt.Tag!.Name
            })
            .ToList();

        return Result<List<TransactionTagDto>>.Success(tags, "Transaction tags retrieved successfully.");
    }


    private TransactionAttachmentDto MapToDtoWithUrl(TransactionAttachment attachment)
    {
        var request = _httpContextAccessor.HttpContext!.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";

        return new TransactionAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName ?? string.Empty,
            FileUrl = $"{baseUrl}/attachments/{attachment.FilePath}"
        };
    }
}

