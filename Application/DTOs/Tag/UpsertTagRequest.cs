using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Tag;
public class UpsertTagRequest
{
    [Required(ErrorMessage = "Tag name is required.")]
    public string Name { get; set; } = string.Empty;
}
