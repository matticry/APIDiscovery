using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class SequenceDto
{
    public int IdSequence { get; set; }
    [Required(ErrorMessage = "El punto de emisión es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El punto de emisión debe ser mayor a 0")]
    public int IdEmissionPoint { get; set; }
    
    [Required(ErrorMessage = "El tipo de documento es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de documento debe ser mayor a 0")]
    public int IdDocumentType { get; set; }
    
    [Required(ErrorMessage = "El código es requerido")]
    [MaxLength(250, ErrorMessage = "El código no puede exceder 250 caracteres")]
    [MinLength(1, ErrorMessage = "El código no puede estar vacío")]
    public string Code { get; set; }
    
}