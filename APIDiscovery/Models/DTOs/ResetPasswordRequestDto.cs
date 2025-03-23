using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string Code { get; set; }
    
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; }
}