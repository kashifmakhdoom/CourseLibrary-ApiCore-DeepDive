using CourseLibrary.API.Models;
using Microsoft.AspNetCore.Connections.Features;
using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.ValidationAttributes
{
    public class CourseTitleMustBeDifferentFromDescriptionAttribute : ValidationAttribute
    {
        public CourseTitleMustBeDifferentFromDescriptionAttribute()
        {
                
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {

            // Check to see the attribute should only be applicable to the CourseForManipulationDTO and derived classes
            if(validationContext.ObjectInstance is not CourseForManipulationDTO course)
            {
                throw new Exception($"Attibute" +
                    $"{nameof(CourseTitleMustBeDifferentFromDescriptionAttribute)}" +
                    $"must be applied to a " +
                    $"{nameof(CourseForManipulationDTO)} or derived types");
            }

            // Custom validation check
            if (course.Title == course.Description)
            {
                return new ValidationResult(
                    "The provided description should be different from the title.",
                    new[] { nameof(CourseForManipulationDTO) });
            }

            return ValidationResult.Success;
        }
    }
}
