using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Transaction;
public class UpsertTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public string? Description { get; set; }

    [Required]
    public int AccountId { get; set; }

    public int? CategoryId { get; set; }

    public List<int> TagIds { get; set; } = [];
    public List<UpsertTransactionSplitRequest> Splits { get; set; } = [];
}