using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class Account : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty; 
    public AccountType Type { get; set; } 
    public decimal CurrentBalance { get; set; } = 0; 
    public bool IsActive { get; set; } = true; 
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool IsDeleted { get; set; }
    public int? CreateBy { get; set; }
    public DateTime? CreateAt { get; set; }
    public int? UpdateBy { get; set; }
    public DateTime? UpdateAt { get; set; }
    public int? DeleteBy { get; set; }
    public DateTime? DeleteAt { get; set; }
}