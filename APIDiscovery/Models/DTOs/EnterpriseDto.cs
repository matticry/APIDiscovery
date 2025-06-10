using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class EnterpriseDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "El nombre de la empresa es requerido")]
    [MaxLength(250, ErrorMessage = "El nombre de la empresa no puede exceder 250 caracteres")]
    public string CompanyName { get; set; }
    
    [Required(ErrorMessage = "El nombre comercial es requerido")]
    [MaxLength(250, ErrorMessage = "El nombre comercial no puede exceder 250 caracteres")]
    public string ComercialName { get; set; }
    
    [Required(ErrorMessage = "El RUC es requerido")]
    [MaxLength(13, ErrorMessage = "El RUC no puede exceder 13 caracteres")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "El RUC debe tener exactamente 13 dígitos")]
    public string Ruc { get; set; }
    
    [Required(ErrorMessage = "La dirección matriz es requerida")]
    [MaxLength(250, ErrorMessage = "La dirección matriz no puede exceder 250 caracteres")]
    public string AddressMatriz { get; set; }
    
    [Required(ErrorMessage = "El teléfono es requerido")]
    [MaxLength(100, ErrorMessage = "El teléfono no puede exceder 100 caracteres")]
    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    public string Phone { get; set; }
    
    [Required(ErrorMessage = "El email es requerido")]
    [MaxLength(250, ErrorMessage = "El email no puede exceder 250 caracteres")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "El tipo de contribuyente especial es requerido")]
    [MaxLength(5, ErrorMessage = "El tipo de contribuyente especial no puede exceder 5 caracteres")]
    [RegularExpression(@"^[SN]$", ErrorMessage = "El contribuyente especial debe ser 'S' (Sí) o 'N' (No)")]
    public string SpecialTaxpayer { get; set; }
    
    [Required(ErrorMessage = "El campo contador es requerido")]
    [RegularExpression(@"^[YN]$", ErrorMessage = "El campo contador debe ser 'Y' (Sí) o 'N' (No)")]
    public char Accountant { get; set; }
    
    [Required(ErrorMessage = "El usuario de email es requerido")]
    [MaxLength(250, ErrorMessage = "El usuario de email no puede exceder 250 caracteres")]
    [EmailAddress(ErrorMessage = "El formato del email de usuario no es válido")]
    public string EmailUser { get; set; }
    
    [Required(ErrorMessage = "La contraseña de email es requerida")]
    [MaxLength(250, ErrorMessage = "La contraseña de email no puede exceder 250 caracteres")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string EmailPassword { get; set; }
    
    [Required(ErrorMessage = "El puerto de email es requerido")]
    [MaxLength(5, ErrorMessage = "El puerto de email no puede exceder 5 caracteres")]
    [RegularExpression(@"^\d{1,5}$", ErrorMessage = "El puerto debe ser un número válido")]
    [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
    public string EmailPort { get; set; }
    
    [Required(ErrorMessage = "El servidor SMTP es requerido")]
    [MaxLength(150, ErrorMessage = "El servidor SMTP no puede exceder 150 caracteres")]
    public string EmailSmtp { get; set; }
    
    [Required(ErrorMessage = "La configuración de seguridad de email es requerida")]
    public bool EmailSecurity { get; set; }
    
    [MaxLength(250, ErrorMessage = "El logo no puede exceder 250 caracteres")]
    public string? Logo { get; set; }
    
    [MaxLength(250, ErrorMessage = "El agente de retención no puede exceder 250 caracteres")]
    public string? RetentionAgent { get; set; }
    
    [Required(ErrorMessage = "El ambiente es requerido")]
    [Range(0, 2, ErrorMessage = "El ambiente debe ser 0 (Desarrollo), 1 (Pruebas) o 2 (Producción)")]
    public int Environment { get; set; }
}