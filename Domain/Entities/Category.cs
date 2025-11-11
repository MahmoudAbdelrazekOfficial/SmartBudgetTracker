using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class Category : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; } //  Expense or Income
    public string? Icon { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Self-referencing relationship for sub-categories
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public bool IsDeleted { get; set; }
    public int? CreateBy { get; set; }
    public DateTime? CreateAt { get; set; }
    public int? UpdateBy { get; set; }
    public DateTime? UpdateAt { get; set; }
    public int? DeleteBy { get; set; }
    public DateTime? DeleteAt { get; set; }
}