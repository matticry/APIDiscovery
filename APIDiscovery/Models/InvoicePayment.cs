using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;
[Table("tbl_invoice_payment")]
public class InvoicePayment
{
    [Key]
    public int id_i_p { get; set; }

    public int id_invoice { get; set; }
    
    [ForeignKey("id_invoice")]
    public virtual Invoice Invoice { get; set; }

    public int id_payment { get; set; }
    
    public decimal total { get; set; }

    public int deadline { get; set; }

    [MaxLength(100)]
    public string unit_time { get; set; } = "dias";
}