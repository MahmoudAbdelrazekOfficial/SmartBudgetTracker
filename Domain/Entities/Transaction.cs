using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class Transaction : BaseEntity, IAuditableEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public bool IsSplit { get; set; } = false;

    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<TransactionSplit> Splits { get; set; } = new List<TransactionSplit>();
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
    public ICollection<TransactionAttachment> Attachments { get; set; } = new List<TransactionAttachment>();

    public int? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }

    public bool IsDeleted { get; set; }
    public int? CreateBy { get; set; }
    public DateTime? CreateAt { get; set; }
    public int? UpdateBy { get; set; }
    public DateTime? UpdateAt { get; set; }
    public int? DeleteBy { get; set; }
    public DateTime? DeleteAt { get; set; }
}