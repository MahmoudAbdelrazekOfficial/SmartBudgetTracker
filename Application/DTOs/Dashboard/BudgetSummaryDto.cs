using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Dashboard;
public class BudgetSummaryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualSpending { get; set; }
    public decimal RemainingAmount => BudgetedAmount - ActualSpending;
}
