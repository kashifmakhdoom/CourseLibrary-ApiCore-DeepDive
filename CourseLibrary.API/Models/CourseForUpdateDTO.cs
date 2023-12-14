using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    public class CourseForUpdateDTO : CourseForManipulationDTO
    {
        [Required(ErrorMessage = "You should fill out a description.")]
        public override string Description { 
            get => base.Description; 
            set => base.Description = value; 
        }
    }
}
