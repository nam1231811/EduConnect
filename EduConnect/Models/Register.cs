﻿using System.ComponentModel.DataAnnotations;

namespace EduConnect.Models
{
    public class Register
    {
        [Required]public string? Email { get; set; } = string.Empty;
        [Required]public string? Password { get; set; } = string.Empty;
    }
}
