using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Account;
public class UpdateAccountRequest
{
    [Required(ErrorMessage = "Account name is required.")]
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
}