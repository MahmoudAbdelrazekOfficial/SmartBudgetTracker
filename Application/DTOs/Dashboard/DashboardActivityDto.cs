using Application.DTOs.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Dashboard;
public class DashboardActivityDto
{
    public List<UpcomingBillDto> UpcomingBills { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}