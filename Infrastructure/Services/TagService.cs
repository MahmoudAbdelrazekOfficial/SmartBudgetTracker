using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Tag;
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
public class TagService(
    IUnitOfWork _unitOfWork,
    IMapper _mapper,
    ICurrentUserService _currentUserService,
    IChangeLogService _changeLogService
    ) : ITagService
{
    public async Task<PagedList<TagDto>> GetTagsAsync(PaginationParams paginationParams)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return new PagedList<TagDto>(
                new List<TagDto>(), 0, paginationParams.PageNumber, paginationParams.PageSize);
        }

        var query = _unitOfWork.Repository<Tag>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value)
            .OrderBy(t => t.Name);

        var pagedResult = await query
            .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(paginationParams.PageNumber, paginationParams.PageSize);

        return pagedResult;
    }

    public async Task<Result<List<TagDto>>> GetTagLookupAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<List<TagDto>>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var tags = await _unitOfWork.Repository<Tag>()
            .GetQueryable()
            .Where(t => t.UserId == userId.Value)
            .OrderBy(t => t.Name)
            .ProjectTo<TagDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<List<TagDto>>.Success(tags);
    }

    public async Task<Result<TagDto>> GetTagByIdAsync(int tagId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<TagDto>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var tag = await _unitOfWork.Repository<Tag>().FindAsync(t => t.Id == tagId);

        if (tag is null)
            return Result<TagDto>.Failure("Tag not found.", HttpStatusCode.NotFound);

        if (tag.UserId != userId.Value)
            return Result<TagDto>.Failure("Tag not found.", HttpStatusCode.NotFound);

        var tagDto = _mapper.Map<TagDto>(tag);

        return Result<TagDto>.Success(tagDto);
    }
    
    public async Task<Result<int>> CreateTagAsync(UpsertTagRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<int>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var existingTag = await _unitOfWork.Repository<Tag>().GetAsync(t =>
            t.UserId == userId.Value &&
            t.Name.ToLower() == request.Name.ToLower()
        );

        if (existingTag.Any())
        {
            return Result<int>.Failure($"A tag with the name '{request.Name}' already exists.", HttpStatusCode.BadRequest);
        }

        var tag = _mapper.Map<Tag>(request);
        tag.UserId = userId.Value;
        _changeLogService.SetCreateChangeLogInfo(tag);

        var createdTag = await _unitOfWork.Repository<Tag>().AddAsync(tag);

        return Result<int>.Success(createdTag.Id, "Tag created successfully.", HttpStatusCode.Created);
    }

    public async Task<Result<bool>> UpdateTagAsync(int tagId, UpsertTagRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var tagToUpdate = await _unitOfWork.Repository<Tag>().FindAsync(t => t.Id == tagId);

        if (tagToUpdate is null)
            return Result<bool>.Failure("Tag not found.", HttpStatusCode.NotFound);

        if (tagToUpdate.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to update this tag.", HttpStatusCode.Forbidden);

        var existingTag = await _unitOfWork.Repository<Tag>().GetAsync(t =>
            t.Id != tagId &&
            t.UserId == userId.Value &&
            t.Name.ToLower() == request.Name.ToLower()
        );

        if (existingTag.Any())
        {
            return Result<bool>.Failure($"Another tag with the name '{request.Name}' already exists.", HttpStatusCode.BadRequest);
        }

        _mapper.Map(request, tagToUpdate);
        _changeLogService.SetUpdateChangeLogInfo(tagToUpdate);

        await _unitOfWork.Repository<Tag>().UpdateAsync(tagToUpdate);

        return Result<bool>.Success(true, "Tag updated successfully.", HttpStatusCode.OK);
    }

    public async Task<Result<bool>> DeleteTagAsync(int tagId)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return Result<bool>.Failure("User not authenticated.", HttpStatusCode.Unauthorized);

        var tagToDelete = await _unitOfWork.Repository<Tag>().FindAsync(t => t.Id == tagId);

        if (tagToDelete is null)
            return Result<bool>.Failure("Tag not found.", HttpStatusCode.NotFound);

        if (tagToDelete.UserId != userId.Value)
            return Result<bool>.Failure("You do not have permission to delete this tag.", HttpStatusCode.Forbidden);

        var isTagUsed = await _unitOfWork.Repository<Transaction>()
            .GetQueryable()
            .AnyAsync(t => t.TransactionTags.Any(tt => tt.TagId == tagId));

        if (isTagUsed)
        {
            return Result<bool>.Failure("Cannot delete this tag because it is associated with existing transactions.", HttpStatusCode.BadRequest);
        }


        await _unitOfWork.Repository<Tag>().DeleteAsync(tagToDelete);

        return Result<bool>.Success(true, "Tag deleted successfully.", HttpStatusCode.OK);
    }
}
