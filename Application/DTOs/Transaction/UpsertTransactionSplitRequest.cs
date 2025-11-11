using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Transaction;
public class UpsertTransactionSplitRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Split amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string? Description { get; set; }
}
