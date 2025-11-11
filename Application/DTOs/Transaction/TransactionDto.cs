using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Transaction;
public class TransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public bool IsSplit { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<TransactionSplitDto> Splits { get; set; } = [];
    public List<int> TagIds { get; set; } = [];
    public List<TransactionAttachmentDto> Attachments { get; set; } = []; 
}

