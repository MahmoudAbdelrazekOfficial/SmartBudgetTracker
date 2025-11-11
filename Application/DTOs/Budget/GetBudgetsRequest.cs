using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Budget;
public class GetBudgetsRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
}