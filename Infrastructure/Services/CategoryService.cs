using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Category;
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
public class CategoryService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
     IChangeLogService _changeLogService
    ) : ICategoryService
{
    public async Task<PagedList<CategoryDto>> GetCategoriesAsync(PaginationParams paginationParams, TransactionType? type)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return new PagedList<CategoryDto>(new List<CategoryDto>(), 0, paginationParams.PageNumber, paginationParams.PageSize);

        var query = _unitOfWork.Repository<Category>()
            .GetQueryable()
            .Where(c => !c.IsDeleted && c.ParentCategoryId == null && 
                        (c.UserId == userId.Value || c.UserId == null)); 
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        var pagedResult = await query
            .OrderBy(c => c.Name)
            .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(paginationParams.PageNumber, paginationParams.PageSize);

        return pagedResult;
    }
    public async Task<Result<CategoryDetailsDto>> GetCategoryByIdAsync(int categoryId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<CategoryDetailsDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var category = await _unitOfWork.Repository<Category>()
            .GetQueryable()
            .Include(c => c.ParentCategory) 
            .Include(c => c.SubCategories)  
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

        if (category is null)
            return Result<CategoryDetailsDto>.Failure("Category not found.", HttpStatusCode.NotFound);

        if (category.UserId != null && category.UserId != userId.Value)
            return Result<CategoryDetailsDto>.Failure("Category not found.", HttpStatusCode.NotFound);

        var categoryDto = _mapper.Map<CategoryDetailsDto>(category);

        return Result<CategoryDetailsDto>.Success(categoryDto);
    }
    public async Task<Result<List<CategoryLookupDto>>> GetCategoryLookupAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<CategoryLookupDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var categories = await _unitOfWork.Repository<Category>()
            .GetQueryable()
            .Where(c => !c.IsDeleted && (c.UserId == userId.Value || c.UserId == null))
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.Name)
            .ProjectTo<CategoryLookupDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<List<CategoryLookupDto>>.Success(categories);
    }
    public async Task<Result<int>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var existingCategory = await _unitOfWork.Repository<Category>().GetAsync(c =>
            c.UserId == userId.Value &&
            c.Name.ToLower() == request.Name.ToLower() &&
            c.Type == request.Type &&
            !c.IsDeleted
        );

        if (existingCategory.Any())
        {
            return Result<int>.Failure($"A category with the name '{request.Name}' and type '{request.Type}' already exists.", HttpStatusCode.BadRequest);
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parentCategory = await _unitOfWork.Repository<Category>().FindAsync(c =>
                c.Id == request.ParentCategoryId.Value && !c.IsDeleted);

            if (parentCategory == null || parentCategory.UserId != userId.Value)
            {
                return Result<int>.Failure("The specified parent category was not found.", HttpStatusCode.BadRequest);
            }
        }

        var category = _mapper.Map<Category>(request);
        category.UserId = userId.Value;
         _changeLogService.SetCreateChangeLogInfo(category);

        var createdCategory = await _unitOfWork.Repository<Category>().AddAsync(category);

        return Result<int>.Success(createdCategory.Id, "Category created successfully.", HttpStatusCode.Created);
    }
    public async Task<Result<bool>> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var categoryToUpdate = await _unitOfWork.Repository<Category>().FindAsync(c => c.Id == categoryId && !c.IsDeleted);

        if (categoryToUpdate is null)
            return Result<bool>.Failure("Category not found.", HttpStatusCode.NotFound);

        if (categoryToUpdate.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to update this category.", HttpStatusCode.Forbidden);

        var existingCategory = await _unitOfWork.Repository<Category>().GetAsync(c =>
            c.Id != categoryId && 
            c.UserId == userId.Value &&
            c.Name.ToLower() == request.Name.ToLower() &&
            c.Type == categoryToUpdate.Type && 
            !c.IsDeleted
        );

        if (existingCategory.Any())
        {
            return Result<bool>.Failure($"Another category with the name '{request.Name}' already exists.", HttpStatusCode.BadRequest);
        }
        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == categoryId)
            {
                return Result<bool>.Failure("A category cannot be its own parent.", HttpStatusCode.BadRequest);
            }

            var parentCategory = await _unitOfWork.Repository<Category>().FindAsync(c =>
                c.Id == request.ParentCategoryId.Value && !c.IsDeleted);

            if (parentCategory == null || parentCategory.UserId != userId.Value)
            {
                return Result<bool>.Failure("The specified parent category was not found.", HttpStatusCode.BadRequest);
            }
        }

        _mapper.Map(request, categoryToUpdate);
        _changeLogService.SetUpdateChangeLogInfo(categoryToUpdate);

        await _unitOfWork.Repository<Category>().UpdateAsync(categoryToUpdate);

        return Result<bool>.Success(true, "Category updated successfully.", HttpStatusCode.OK);
    }
    public async Task<Result<bool>> DeleteCategoryAsync(int categoryId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var categoryToDelete = await _unitOfWork.Repository<Category>().FindAsync(c => c.Id == categoryId && !c.IsDeleted);

        if (categoryToDelete is null)
            return Result<bool>.Failure("Category not found.", HttpStatusCode.NotFound);

        if (categoryToDelete.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to delete this category.", HttpStatusCode.Forbidden);

        var isCategoryUsedInTransactions = await _unitOfWork.Repository<Transaction>()
            .GetAsync(t => t.CategoryId == categoryId && !t.IsDeleted);

        if (isCategoryUsedInTransactions.Any())
            return Result<bool>.Failure("Cannot delete this category because it is associated with existing transactions.", HttpStatusCode.BadRequest);

        var hasSubCategories = await _unitOfWork.Repository<Category>()
            .GetAsync(c => c.ParentCategoryId == categoryId && !c.IsDeleted);

        if (hasSubCategories.Any())
            return Result<bool>.Failure("Cannot delete this category because it has active sub-categories. Please delete them first.", HttpStatusCode.BadRequest);

        categoryToDelete.IsDeleted = true;
        _changeLogService.SetDeleteChangeLogInfo(categoryToDelete); 

        await _unitOfWork.Repository<Category>().UpdateAsync(categoryToDelete);

        return Result<bool>.Success(true, "Category deleted successfully.", HttpStatusCode.OK);
    }
}
