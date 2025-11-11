using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Category;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common;

namespace Application.Interfaces;
public interface ICategoryService
{
    Task<PagedList<CategoryDto>> GetCategoriesAsync(PaginationParams paginationParams, TransactionType? type);
    Task<Result<CategoryDetailsDto>> GetCategoryByIdAsync(int categoryId);
    Task<Result<List<CategoryLookupDto>>> GetCategoryLookupAsync();
    Task<Result<int>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<Result<bool>> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request);
    Task<Result<bool>> DeleteCategoryAsync(int categoryId);
}
