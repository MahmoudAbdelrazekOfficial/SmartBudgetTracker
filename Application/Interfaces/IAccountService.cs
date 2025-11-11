using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Account;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IAccountService
{
    Task<PagedList<AccountDto>> GetAccountsAsync(PaginationParams paginationParams);
    Task<Result<AccountDto>> GetAccountByIdAsync(int accountId);
    Task<Result<int>> CreateAccountAsync(CreateAccountRequest request);
    Task<Result<bool>> UpdateAccountAsync(int accountId, UpdateAccountRequest request);
    Task<Result<bool>> SoftDeleteAccountAsync(int accountId);
    Task<Result<bool>> ToggleAccountStatusAsync(int accountId);

}
