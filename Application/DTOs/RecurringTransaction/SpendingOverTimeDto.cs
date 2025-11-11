using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.RecurringTransaction;
public class SpendingOverTimeDto
{
    public string Period { get; set; } = string.Empty; 
    public decimal TotalSpending { get; set; }
}

