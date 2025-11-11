using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Budget;
public class BudgetProgressDto
{
    public int BudgetId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetedAmount { get; set; }
    public decimal ActualSpending { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsOverBudget { get; set; }
    public double ProgressPercentage { get; set; }
}