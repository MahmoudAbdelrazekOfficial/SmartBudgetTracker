using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Transfere;
public class TransferDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Description { get; set; }
    public int FromAccountId { get; set; }
    public string FromAccountName { get; set; } = string.Empty;
    public int ToAccountId { get; set; }
    public string ToAccountName { get; set; } = string.Empty;
}
