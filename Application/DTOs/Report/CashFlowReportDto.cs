using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Report;
public class CashFlowReportDto
{
    public string Period { get; set; } = string.Empty; 
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetResult => TotalIncome - TotalExpense;
}
