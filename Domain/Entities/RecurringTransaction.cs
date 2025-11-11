using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class RecurringTransaction : BaseEntity, IAuditableEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } 
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NextDueDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true; 

    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int FrequencyId { get; set; }
    public Frequency? Frequency { get; set; }

    // --- Auditing ---
    public bool IsDeleted { get; set; } = false;
    public int? CreateBy { get; set; }
    public DateTime? CreateAt { get; set; }
    public int? UpdateBy { get; set; }
    public DateTime? UpdateAt { get; set; }
    public int? DeleteBy { get; set; }
    public DateTime? DeleteAt { get; set; }
}
