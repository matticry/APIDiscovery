using System.ComponentModel.DataAnnotations;

namespace APIDiscovery.Models.DTOs.CreditNoteDTOs;

public class CreditNoteDetailRequestDTO
{
    [Required]
    public int ArticleId { get; set; }

    [Required]
    public int TariffId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }

    // Para corrección de precios
    public decimal? NewPriceUnit { get; set; }

    // Para corrección de descuentos
    public decimal? NewDiscount { get; set; }

    [StringLength(255)]
    public string? Note1 { get; set; }

    [StringLength(255)]
    public string? Note2 { get; set; }

    [StringLength(255)]
    public string? Note3 { get; set; }
}