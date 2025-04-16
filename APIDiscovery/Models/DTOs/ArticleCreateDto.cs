using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs;

public class ArticleCreateDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Name { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }

    [Required(ErrorMessage = "El precio unitario es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal PriceUnit { get; set; }

    [Required(ErrorMessage = "El stock es requerido")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0")]
    public int Stock { get; set; }

    public IFormFile? Image { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "El ID de empresa es requerido")]
    public int IdEnterprise { get; set; }

    [Required(ErrorMessage = "El ID de categoría es requerido")]
    public int IdCategory { get; set; }

    [Required(ErrorMessage = "Debe seleccionar al menos una tarifa")]
    public List<int> FareIds { get; set; }

    // 👇 NUEVA PROPIEDAD para recibir la imagen como archivo
}