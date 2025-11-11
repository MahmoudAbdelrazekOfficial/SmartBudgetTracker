using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class TransactionSplit : BaseEntity
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}