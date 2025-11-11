using Application.DTOs.Dashboard;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync();
    Task<Result<DashboardActivityDto>> GetDashboardActivityAsync();
}
