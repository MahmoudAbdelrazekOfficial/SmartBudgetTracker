using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Tag;
using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Application.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SmartBudgetTracker.Api.Controllers;
[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = AppRoles.User)]
public class TagsController(ITagService _tagService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<TagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags([FromQuery] PaginationParams paginationParams)
    {
        var result = await _tagService.GetTagsAsync(paginationParams);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<TagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagLookup()
    {
        var result = await _tagService.GetTagLookupAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TagDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTagById(int id)
    {
        var result = await _tagService.GetTagByIdAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] UpsertTagRequest request)
    {
        var result = await _tagService.CreateTagAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpsertTagRequest request)
    {
        var result = await _tagService.UpdateTagAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await _tagService.DeleteTagAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }
}
