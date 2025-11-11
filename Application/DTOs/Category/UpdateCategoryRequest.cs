using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Category;
public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public int? ParentCategoryId { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }
}