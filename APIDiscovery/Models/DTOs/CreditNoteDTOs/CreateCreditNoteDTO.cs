using System.ComponentModel.DataAnnotations;
using APIDiscovery.Models.Enum;

namespace APIDiscovery.Models.DTOs.CreditNoteDTOs;

public class CreateCreditNoteDTO
{
    [Required]
    public int InvoiceOriginalId { get; set; }

    [Required]
    [StringLength(300)]
    public string Motive { get; set; }

    [Required]
    public CreditNoteType CreditNoteType { get; set; }

    // Para anulación parcial o corrección
    public List<CreditNoteDetailRequestDTO>? Details { get; set; }

    [StringLength(300)]
    public string? AdditionalInfo { get; set; }

    [StringLength(255)]
    public string? Message { get; set; }
}