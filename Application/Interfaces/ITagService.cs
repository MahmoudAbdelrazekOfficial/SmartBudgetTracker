using Application.Common.Pagination;
using Application.Common;
using Application.DTOs.Tag;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface ITagService
{
    Task<PagedList<TagDto>> GetTagsAsync(PaginationParams paginationParams);
    Task<Result<TagDto>> GetTagByIdAsync(int tagId);
    Task<Result<List<TagDto>>> GetTagLookupAsync();
    Task<Result<int>> CreateTagAsync(UpsertTagRequest request);
    Task<Result<bool>> UpdateTagAsync(int tagId, UpsertTagRequest request);
    Task<Result<bool>> DeleteTagAsync(int tagId);
}
