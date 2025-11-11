using Application.Interfaces;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services;
public class ChangeLogService(ICurrentUserService currentUserService) : IChangeLogService
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public void SetCreateChangeLogInfo<T>(T entity) where T : IAuditableEntity
    {
        entity.CreateBy = _currentUserService.UserId;
        entity.CreateAt = DateTime.UtcNow;
    }

    public void SetUpdateChangeLogInfo<T>(T entity) where T : IAuditableEntity
    {
        entity.UpdateBy = _currentUserService.UserId;
        entity.UpdateAt = DateTime.UtcNow;
    }

    public void SetDeleteChangeLogInfo<T>(T entity) where T : IAuditableEntity
    {
        entity.DeleteBy = _currentUserService.UserId;
        entity.DeleteAt = DateTime.UtcNow;
    }
}