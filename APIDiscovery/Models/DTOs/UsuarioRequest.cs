using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class UsuarioRequest
{
    [Required]
    public string name_us { get; set; }

    [Required]
    public string lastname_us { get; set; }

    [Required]
    [EmailAddress]
    public string email_us { get; set; }

    [Required]
    public string password_us { get; set; }

    [Required]
    public string empresa { get; set; } 

    [Required]
    public string rol { get; set; } 

    public string dni_us { get; set; }
    public IFormFile? image_us { get; set; }
}