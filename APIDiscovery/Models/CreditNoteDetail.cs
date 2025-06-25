using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Models;

[Table("tbl_credit_note_detail")]
public class CreditNoteDetail
{
    [Key]
    [Column("id_c_n_d")]
    public int IdCreditNoteDetail { get; set; }

    [Column("note_credit_id")]
    public int? NoteCreditId { get; set; }

    // Campos del producto
    [Column("code_stub")]
    [StringLength(50)]
    [Required]
    public string CodeStub { get; set; }

    [Column("description")]
    [StringLength(255)]
    [Required]
    public string Description { get; set; }

    [Column("amount")]
    [Required]
    public int Amount { get; set; }

    [Column("price_unit")]
    [Precision(10, 4)]
    [Required]
    public decimal PriceUnit { get; set; }

    [Column("discount")]
    [Precision(10, 2)]
    public decimal Discount { get; set; } = 0.00m;

    // Campos calculados
    [Column("neto")]
    [Precision(10, 2)]
    public decimal Neto { get; set; } = 0.00m;

    [Column("iva_porc")]
    [Precision(5, 2)]
    public decimal IvaPorc { get; set; } = 0.00m;

    [Column("ice_porc")]
    [Precision(5, 2)]
    public decimal IcePorc { get; set; } = 0.00m;

    [Column("iva_valor")]
    [Precision(10, 2)]
    public decimal IvaValor { get; set; } = 0.00m;

    [Column("ice_valor")]
    [Precision(10, 2)]
    public decimal IceValor { get; set; } = 0.00m;

    [Column("subtotal")]
    [Precision(10, 2)]
    public decimal Subtotal { get; set; } = 0.00m;

    [Column("total")]
    [Precision(10, 2)]
    public decimal Total { get; set; } = 0.00m;

    // Notas adicionales
    [Column("nota1")]
    [StringLength(255)]
    public string Nota1 { get; set; }

    [Column("nota2")]
    [StringLength(255)]
    public string Nota2 { get; set; }

    [Column("nota3")]
    [StringLength(255)]
    public string Nota3 { get; set; }

    // Foreign Keys
    [Column("id_tariff")]
    public int? IdTariff { get; set; }

    [Column("id_article")]
    public int? IdArticle { get; set; }

    // Campos de auditoría
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.Now;

    // Navegación - Relaciones
    [ForeignKey("NoteCreditId")]
    public virtual CreditNote CreditNote { get; set; }

    [ForeignKey("IdTariff")]
    public virtual Fare Tariff { get; set; }

    [ForeignKey("IdArticle")]
    public virtual Article Article { get; set; }
}