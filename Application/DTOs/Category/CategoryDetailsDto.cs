using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Category;
public class CategoryDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string? Icon { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new(); 
}