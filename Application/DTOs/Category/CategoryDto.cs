using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Category;
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string? Icon { get; set; }
    public bool HasSubCategories { get; set; } 
}