/* 
 *  Copyright © 2024 Travelonium AB
 *  
 *  This file is part of Arcadeia.
 *  
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *  
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *  
 */

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