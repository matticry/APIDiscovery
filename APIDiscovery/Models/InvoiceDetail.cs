using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_invoice_detail")]
public class InvoiceDetail
{
    [Key]
    public int id_i_d { get; set; }

    [MaxLength(250)]
    public string code_stub { get; set; }

    public string description { get; set; }

    public int amount { get; set; }
    
    [Column(TypeName = "decimal(10, 2)")]
    public decimal price_unit { get; set; }

    public int discount { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal price_with_discount { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal neto { get; set; }
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal iva_porc { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal iva_valor { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal ice_porc { get; set; }
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal ice_valor { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal irbp_valor { get; set; }
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal subtotal { get; set; }
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal total { get; set; }

    public int id_invoice { get; set; }

    public string note1 { get; set; }

    public string note2 { get; set; }

    public string note3 { get; set; }

    public int id_tariff { get; set; }

    public int id_article { get; set; }
}