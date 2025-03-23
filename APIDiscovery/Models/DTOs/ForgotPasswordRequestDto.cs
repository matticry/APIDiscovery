using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
