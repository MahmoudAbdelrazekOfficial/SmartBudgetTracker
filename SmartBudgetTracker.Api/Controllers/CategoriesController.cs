using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Category;
using Application.Interfaces;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class CategoriesController(ICategoryService _categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories([FromQuery] PaginationParams paginationParams, [FromQuery] TransactionType? type)
    {
        var result = await _categoryService.GetCategoriesAsync(paginationParams, type);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<CategoryLookupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryLookup()
    {
        var result = await _categoryService.GetCategoryLookupAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<CategoryDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CategoryDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateCategoryAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
