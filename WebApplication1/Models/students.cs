using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Students
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive integer greater than zero.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Name must contain only letters and spaces.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[a-z][a-z0-9._%+-]*@gmail\.com$",
            ErrorMessage = "Email must be lowercase, contain no special characters except . _ % + -, and end with @gmail.com.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^(?!.*[^0-9]).{10}$",
            ErrorMessage = "Mobile number must be exactly 10 digits and special characters are not allowed.")]
        public string Mobileno { get; set; } = string.Empty;
    }
}
