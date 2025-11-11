using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;
public class TransactionAttachment : BaseEntity
{
    public string FilePath { get; set; } = string.Empty;
    public string? FileName { get; set; }

    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public DateTime UploadedAt { get; set; }
}