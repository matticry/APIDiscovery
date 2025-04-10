using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APIDiscovery.Models;

[Table("tbl_enterprise_user")]
public class EnterpriseUser
{
    [Key]
    public int id_e_u { get; set; }
    
    public int id_user { get; set; }
    
    public int id_enterprise { get; set; }
    
    public char status { get; set; } = 'A';
    
    public DateTime? start_date_subscription { get; set; }
    
    public DateTime? end_date_subscription { get; set; }
    
    [ForeignKey("id_user")]
    public Usuario Usuario { get; set; }
    
    [ForeignKey("id_enterprise")]
    public Enterprise Enterprise { get; set; }
}