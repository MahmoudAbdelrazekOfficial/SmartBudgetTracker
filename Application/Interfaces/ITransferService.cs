using Application.Common.Pagination;
using Application.DTOs.Transfere;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface ITransferService
{
    Task<PagedList<TransferDto>> GetTransfersAsync(GetTransfersRequest request);
    Task<Result<TransferDto>> GetTransferByIdAsync(int transferId);
    Task<Result<int>> CreateTransferAsync(UpsertTransferRequest request);
    Task<Result<bool>> UpdateTransferAsync(int transferId, UpsertTransferRequest request);
    Task<Result<bool>> DeleteTransferAsync(int transferId);
}
