using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class TransactionTag
{
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}