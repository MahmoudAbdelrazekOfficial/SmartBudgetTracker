using Application.DTOs.Budget;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces;
public interface IBudgetService
{
    Task<Result<List<BudgetDto>>> GetBudgetsAsync(GetBudgetsRequest request);
    Task<Result<BudgetDto>> GetBudgetByIdAsync(int budgetId);
    Task<Result<List<BudgetProgressDto>>> GetBudgetProgressAsync(GetBudgetsRequest request);
    Task<Result<List<BudgetSuggestionDto>>> GetBudgetSuggestionsAsync(GetBudgetsRequest request);
    Task<Result<int>> CreateBudgetAsync(UpsertBudgetRequest request);
    Task<Result<bool>> UpdateBudgetAsync(int budgetId, UpsertBudgetRequest request);
    Task<Result<bool>> DeleteBudgetAsync(int budgetId);
    
}
