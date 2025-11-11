using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Transfere;
public class UpsertTransferRequest
{
    [Required(ErrorMessage = "Source account is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid source account ID.")]
    public int FromAccountId { get; set; }

    [Required(ErrorMessage = "Destination account is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid destination account ID.")]
    public int ToAccountId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "The transfer amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Transfer date is required.")]
    public DateTime TransferDate { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
    public string? Description { get; set; }
}
