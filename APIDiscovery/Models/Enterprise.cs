using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_enterprise")]
public class Enterprise
{
    [Key]
    public int id_en { get; set; }
    
    [MaxLength(250)]
    public string company_name { get; set; }
    
    [MaxLength(250)]
    public string comercial_name { get; set; }
    
    [MaxLength(13)]
    public string ruc { get; set; }
    
    [MaxLength(250)]
    public string address_matriz { get; set; }
    
    [MaxLength(100)]
    public string phone { get; set; }
    
    [MaxLength(250)]
    public string email { get; set; }
    
    [MaxLength(5)]
    public string special_taxpayer { get; set; }
    
    public char accountant { get; set; } = 'Y';
    
    [MaxLength(250)]
    public string email_user { get; set; }
    
    [MaxLength(250)]
    public string email_password { get; set; }
    
    [MaxLength(5)]
    public string email_port { get; set; }
    
    [MaxLength(150)]
    public string email_smtp { get; set; }
    
    public int email_security { get; set; }
    
    public int email_type { get; set; }
    
    [MaxLength(250)]
    public string electronic_signature { get; set; }
    
    [MaxLength(250)]
    public string key_signature { get; set; }
    
    [MaxLength(250)]
    public string logo { get; set; }
    
    public DateTime? start_date_signature { get; set; }
    
    public DateTime? end_date_signature { get; set; }
    
    [MaxLength(250)]
    public string retention_agent { get; set; }
    
    public char environment { get; set; }
    
    [JsonIgnore]
    public ICollection<EnterpriseUser> EnterpriseUsers { get; set; }
}