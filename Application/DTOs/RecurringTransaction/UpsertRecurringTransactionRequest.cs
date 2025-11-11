using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.RecurringTransaction;
public class UpsertRecurringTransactionRequest
{
    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "Invalid transaction type.")]
    public TransactionType Type { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [DateGreaterThan(nameof(StartDate), ErrorMessage = "End Date must be after Start Date.")]
    public DateTime? EndDate { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Account ID is required.")]
    public int AccountId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Frequency ID is required.")]
    public int FrequencyId { get; set; }
}

public class DateGreaterThanAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateGreaterThanAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
        if (property == null)
            throw new ArgumentException("Property with this name not found");

        var comparisonValue = (DateTime?)property.GetValue(validationContext.ObjectInstance);
        var currentValue = (DateTime?)value;

        if (currentValue <= comparisonValue)
            return new ValidationResult(ErrorMessage);

        return ValidationResult.Success;
    }
}