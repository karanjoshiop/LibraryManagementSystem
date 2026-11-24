using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models.Dtos
{
    /// <summary>
    /// Class-level validator so the date order is checked even when the inputs
    /// keep their previous value after an unparseable date entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EndDateAfterStartDateAttribute : ValidationAttribute
    {
        public EndDateAfterStartDateAttribute()
            : base("End date cannot be before the start date.")
        {
        }

        protected override ValidationResult? IsValid(
            object? value, ValidationContext validationContext)
        {
            if (value is not MembershipInput input)
                return ValidationResult.Success;

            return input.EndDate < input.StartDate
                ? new ValidationResult(ErrorMessageString, new[] { nameof(MembershipInput.EndDate) })
                : ValidationResult.Success;
        }
    }
}
