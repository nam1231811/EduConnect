﻿using System.ComponentModel.DataAnnotations;

namespace EduConnect.DTO
{
    public class Register
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email must be a Gmail address")]
        public string? Email { get; set; } = string.Empty;
        [Required]public string? Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string>? StudentIds { get; set; }
    }
}
