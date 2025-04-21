using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;

[Table("tbl_sequence")]
public class Sequence
{
    [Key]
    public int id_sequence { get; set; }
        
    public int id_emission_point { get; set; }
        
    public int id_document_type { get; set; }
        
    [MaxLength(250)]
    public string code { get; set; }
        
    [ForeignKey("id_emission_point")]
    public EmissionPoint EmissionPoint { get; set; }
        
}