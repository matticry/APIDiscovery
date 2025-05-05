using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_emission_point")]
public class EmissionPoint
{
    [Key]
    public int id_e_p { get; set; }
        
    [MaxLength(250)]
    public string code { get; set; }
        
    [Column("details ")] // Nota: el espacio en el nombre de columna
    public string details { get; set; }
        
    public bool type { get; set; }
        
    public int id_branch { get; set; }
        
    [ForeignKey("id_branch")]
    public Branch Branch { get; set; }
        
    public ICollection<Sequence> Sequences { get; set; }
}