using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class EmissionPointDto
{
    public int IdEmissionPoint { get; set; }
    [Required(ErrorMessage = "El código es requerido")]
    [MaxLength(250, ErrorMessage = "El código no puede exceder 250 caracteres")]
    [MinLength(1, ErrorMessage = "El código no puede estar vacío")]
    public string Code { get; set; }
    
    [Required(ErrorMessage = "Los detalles son requeridos")]
    [MaxLength(500, ErrorMessage = "Los detalles no pueden exceder 500 caracteres")]
    [MinLength(1, ErrorMessage = "Los detalles no pueden estar vacíos")]
    public string Details { get; set; }
    
    [Required(ErrorMessage = "El tipo es requerido")]
    public bool Type { get; set; }
    
    [Required(ErrorMessage = "La sucursal es requerida")]
    public int IdBranch { get; set; }
}