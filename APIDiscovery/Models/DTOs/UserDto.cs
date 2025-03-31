using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class UserDto
{
    [Key]
    public int id_us { get; set; }
    
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
    public string rol { get; set; } 
    
    [Required]
    [StringLength(10, ErrorMessage = "DNI no puede exceder 10 caracteres")]
    public string dni_us { get; set; }
    public IFormFile? image_us { get; set; }
    
    [Required]
    public DateTime birthday_us { get; set; }
    
    [Required]
    public string phone_us { get; set; }
    
    [Required]
    public string nationality_us { get; set; }
    
    [Required]
    public string gender_us { get; set; }
    
    [Required]
    public char terms_and_conditions { get; set; }
    public int age_us { get; set; }
}