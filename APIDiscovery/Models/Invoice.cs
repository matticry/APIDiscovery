using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_invoice")]
    public class Invoice
    {
        [Key]
        public int inv_id { get; set; }

        public DateTime emission_date { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal total_without_taxes { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal total_discount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal tip { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal total_amount { get; set; }

        [MaxLength(10)]
        public string currency { get; set; }

        public int sequence_id { get; set; }

        [MaxLength(50)]
        public string electronic_status { get; set; }

        [MaxLength(50)]
        public string invoice_status { get; set; }

        public int id_emission_point { get; set; }

        public int company_id { get; set; }

        public int client_id { get; set; }

        [MaxLength(250)]
        public string access_key { get; set; }

        public int branch_id { get; set; }

        public int receipt_id { get; set; }

        public DateTime? authorization_date { get; set; }

        [MaxLength(50)]
        public string authorization_number { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal total_vat { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal total_vat_0 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal vat { get; set; }

        public string message { get; set; }

        public string additional_info { get; set; }

        [MaxLength(250)]
        public string identifier { get; set; }

        [MaxLength(50)]
        public string type { get; set; }

        [MaxLength(50)]
        public string modified_doc_number { get; set; }

        public DateTime? modified_doc_date { get; set; }

        [MaxLength(100)]
        public string sequence { get; set; }


        [ForeignKey("client_id")]
        public virtual Client Client { get; set; }

        [ForeignKey("branch_id")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("sequence_id")]
        public virtual Sequence Sequence { get; set; }

        [ForeignKey("receipt_id")]
        public virtual DocumentType DocumentType { get; set; }

        [ForeignKey("company_id")]
        public virtual Enterprise Enterprise { get; set; }

        [ForeignKey("id_emission_point")]
        public virtual EmissionPoint EmissionPoint { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }
        public virtual ICollection<InvoicePayment> InvoicePayments { get; set; }
    }