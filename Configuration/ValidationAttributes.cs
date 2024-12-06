using System.ComponentModel.DataAnnotations;

namespace Arcadeia.Configuration
{
   public class RegularExpressionsAttribute(string pattern) : ValidationAttribute
   {
      private readonly string _pattern = pattern;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
      {
         if (value is not IEnumerable<string> list)
         {
            return ValidationResult.Success;
         }

         var regex = new System.Text.RegularExpressions.Regex(_pattern);

         foreach (var item in list)
         {
            if (string.IsNullOrWhiteSpace(item) || !regex.IsMatch(item))
            {
               return new ValidationResult(ErrorMessage ?? $"Each item in {validationContext.DisplayName} must match the pattern {_pattern}.");
            }
         }

         return ValidationResult.Success;
      }
   }
}