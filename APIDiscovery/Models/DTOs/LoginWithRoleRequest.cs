using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class LoginWithRoleRequest
{
    [Required]
    
    public string Cedula { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [Required]
    public string Role { get; set; }
}