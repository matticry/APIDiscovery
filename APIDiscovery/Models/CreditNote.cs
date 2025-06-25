using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Models;

[Table("tbl_credit_note")]
public class CreditNote
{
    [Key]
    [Column("id_c_n")]
    public int IdCreditNote { get; set; }

    [Column("emission_date")]
    public DateTime EmissionDate { get; set; } = DateTime.Now;

    [Column("total_without_taxes")]
    [Precision(10, 2)]
    public decimal TotalWithoutTaxes { get; set; } = 0.00m;

    [Column("total_discount")]
    [Precision(10, 2)]
    public decimal TotalDiscount { get; set; } = 0.00m;

    [Column("tip")]
    [Precision(10, 2)]
    public decimal Tip { get; set; } = 0.00m;

    [Column("total_amount")]
    [Precision(10, 2)]
    public decimal TotalAmount { get; set; } = 0.00m;

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    // Campos específicos de Nota de Crédito
    [Column("cod_doc_modificado")]
    [StringLength(2)]
    [Required]
    public string CodDocModificado { get; set; } = "04";

    [Column("num_doc_modificado")]
    [StringLength(50)]
    [Required]
    public string NumDocModificado { get; set; }

    [Column("emission_date_doc_sustento")]
    [Required]
    public DateTime EmissionDateDocSustento { get; set; }

    [Column("modification_value")]
    [Precision(10, 2)]
    public decimal ModificationValue { get; set; } = 0m;

    [Column("motive")]
    [StringLength(300)]
    [Required]
    public string Motive { get; set; }

    // Campos del sistema
    [Column("access_key")]
    [StringLength(50)]
    [Required]
    public string AccessKey { get; set; }

    [Column("authorization_number")]
    [StringLength(50)]
    [Required]
    public string AuthorizationNumber { get; set; }

    [Column("authorization_date")]
    public DateTime? AuthorizationDate { get; set; }

    [Column("electronic_status")]
    [StringLength(20)]
    public string ElectronicStatus { get; set; } = "PENDIENTES";

    [Column("aditional_info")]
    [StringLength(300)]
    public string AditionalInfo { get; set; }

    [Column("message")]
    [StringLength(255)]
    public string Message { get; set; }

    [Column("sequence")]
    [StringLength(100)]
    public string Sequence { get; set; }

    [Column("xml")]
    [StringLength(255)]
    public string Xml { get; set; }

    // Foreign Keys
    [Column("sequence_id")]
    public int? SequenceId { get; set; }

    [Column("id_emission_point")]
    public int? IdEmissionPoint { get; set; }

    [Column("company_id")]
    public int? CompanyId { get; set; }

    [Column("client_id")]
    public int? ClientId { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("receipt_id")]
    public int? ReceiptId { get; set; }

    [Column("invoice_original_id")]
    public int? InvoiceOriginalId { get; set; }

    // Campos de auditoría
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public DateTime? CreatedBy { get; set; } 

    [Column("updated_by")]
    public DateTime? UpdatedBy { get; set; } 

    [ForeignKey("IdEmissionPoint")]
    public virtual EmissionPoint EmissionPoint { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Enterprise Enterprise { get; set; }

    [ForeignKey("ClientId")]
    public virtual Client Client { get; set; }

    [ForeignKey("BranchId")]
    public virtual Branch Branch { get; set; }

    [ForeignKey("ReceiptId")]
    public virtual DocumentType DocumentType { get; set; }

    [ForeignKey("InvoiceOriginalId")]
    public virtual Invoice InvoiceOriginal { get; set; }

    // Colección de detalles
    public virtual ICollection<CreditNoteDetail> CreditNoteDetails { get; set; } = new List<CreditNoteDetail>();
    
    [Column("xml_base64")] public string? XmlBase64 { get; set; }
}