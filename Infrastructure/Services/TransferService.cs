using Application.Common.Pagination;
using Application.DTOs.Transfere;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;
public class TransferService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
    IChangeLogService _changeLogService
    ) : ITransferService
{
    public async Task<PagedList<TransferDto>> GetTransfersAsync(GetTransfersRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return new PagedList<TransferDto>(new List<TransferDto>(), 0, request.PageNumber, request.PageSize);

        var query = _unitOfWork.Repository<Transfer>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value && !t.IsDeleted);

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.TransferDate.Date >= request.StartDate.Value.Date);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.TransferDate.Date <= request.EndDate.Value.Date);
        }

        if (request.AccountId.HasValue)
        {
            query = query.Where(t => t.FromAccountId == request.AccountId.Value || t.ToAccountId == request.AccountId.Value);
        }

        var pagedResult = await query
            .OrderByDescending(t => t.TransferDate)
            .ProjectTo<TransferDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(request.PageNumber, request.PageSize);

        return pagedResult;
    }

    public async Task<Result<TransferDto>> GetTransferByIdAsync(int transferId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<TransferDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transfer = await _unitOfWork.Repository<Transfer>()
            .GetQueryable()
            .Include(t => t.FromAccount) 
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == transferId && !t.IsDeleted);

        if (transfer is null)
            return Result<TransferDto>.Failure("Transfer not found.", HttpStatusCode.NotFound);

        if (transfer.UserId != userId.Value)
            return Result<TransferDto>.Failure("Transfer not found.", HttpStatusCode.NotFound);

        var transferDto = _mapper.Map<TransferDto>(transfer);

        return Result<TransferDto>.Success(transferDto);
    }

    public async Task<Result<int>> CreateTransferAsync(UpsertTransferRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        if (request.FromAccountId == request.ToAccountId)
            return Result<int>.Failure("Source and destination accounts cannot be the same.", HttpStatusCode.BadRequest);

        var fromAccount = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.FromAccountId && a.UserId == userId && !a.IsDeleted);
        var toAccount = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.ToAccountId && a.UserId == userId && !a.IsDeleted);

        if (fromAccount is null)
            return Result<int>.Failure("Source account not found.", HttpStatusCode.BadRequest);
        if (toAccount is null)
            return Result<int>.Failure("Destination account not found.", HttpStatusCode.BadRequest);

        fromAccount.CurrentBalance -= request.Amount;
        toAccount.CurrentBalance += request.Amount;

        var transfer = _mapper.Map<Transfer>(request);
        transfer.UserId = userId.Value;

        _changeLogService.SetCreateChangeLogInfo(transfer);

        await _unitOfWork.Repository<Account>().UpdateAsync(fromAccount);
        await _unitOfWork.Repository<Account>().UpdateAsync(toAccount);
        var createdTransfer = await _unitOfWork.Repository<Transfer>().AddAsync(transfer);

        return Result<int>.Success(createdTransfer.Id, "Transfer created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateTransferAsync(int transferId, UpsertTransferRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transferToUpdate = await _unitOfWork.Repository<Transfer>()
            .GetQueryable()
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.UserId == userId.Value && !t.IsDeleted);

        if (transferToUpdate is null)
            return Result<bool>.Failure("Transfer not found.", HttpStatusCode.NotFound);

        transferToUpdate.FromAccount!.CurrentBalance += transferToUpdate.Amount;
        transferToUpdate.ToAccount!.CurrentBalance -= transferToUpdate.Amount;

        var newFromAccount = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.FromAccountId && a.UserId == userId.Value);
        var newToAccount = await _unitOfWork.Repository<Account>().FindAsync(a => a.Id == request.ToAccountId && a.UserId == userId.Value);

        if (newFromAccount is null || newToAccount is null)
            return Result<bool>.Failure("One of the accounts in the update request was not found.", HttpStatusCode.BadRequest);

        newFromAccount.CurrentBalance -= request.Amount;
        newToAccount.CurrentBalance += request.Amount;

        _mapper.Map(request, transferToUpdate);

        _changeLogService.SetUpdateChangeLogInfo(transferToUpdate);

        await _unitOfWork.Repository<Account>().UpdateAsync(transferToUpdate.FromAccount);
        await _unitOfWork.Repository<Account>().UpdateAsync(transferToUpdate.ToAccount);
        if (transferToUpdate.FromAccountId != newFromAccount.Id) await _unitOfWork.Repository<Account>().UpdateAsync(newFromAccount);
        if (transferToUpdate.ToAccountId != newToAccount.Id) await _unitOfWork.Repository<Account>().UpdateAsync(newToAccount);

        await _unitOfWork.Repository<Transfer>().UpdateAsync(transferToUpdate);

        return Result<bool>.Success(true, "Transfer updated successfully.");
    }

    public async Task<Result<bool>> DeleteTransferAsync(int transferId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var transferToDelete = await _unitOfWork.Repository<Transfer>()
            .GetQueryable()
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.UserId == userId.Value && !t.IsDeleted);

        if (transferToDelete is null)
            return Result<bool>.Failure("Transfer not found.", HttpStatusCode.NotFound);

        transferToDelete.FromAccount!.CurrentBalance += transferToDelete.Amount;
        transferToDelete.ToAccount!.CurrentBalance -= transferToDelete.Amount;

        transferToDelete.IsDeleted = true;

        _changeLogService.SetDeleteChangeLogInfo(transferToDelete);

        await _unitOfWork.Repository<Account>().UpdateAsync(transferToDelete.FromAccount);
        await _unitOfWork.Repository<Account>().UpdateAsync(transferToDelete.ToAccount);
        await _unitOfWork.Repository<Transfer>().UpdateAsync(transferToDelete);

        return Result<bool>.Success(true, "Transfer deleted successfully.");
    }
}
