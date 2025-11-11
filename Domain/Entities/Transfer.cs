using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class Transfer : BaseEntity, IAuditableEntity
{
    public decimal Amount { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Description { get; set; }

    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int FromAccountId { get; set; }
    public Account? FromAccount { get; set; }

    public int ToAccountId { get; set; }
    public Account? ToAccount { get; set; }

    public bool IsDeleted { get; set; }
    public int? CreateBy { get; set; }
    public DateTime? CreateAt { get; set; }
    public int? UpdateBy { get; set; }
    public DateTime? UpdateAt { get; set; }
    public int? DeleteBy { get; set; }
    public DateTime? DeleteAt { get; set; }
}
