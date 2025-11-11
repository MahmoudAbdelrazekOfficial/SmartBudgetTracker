using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Dashboard;
public class DashboardSummaryDto
{
    public decimal TotalBalance { get; set; }
    public List<BudgetSummaryDto> BudgetSummaries { get; set; } = new();
}
