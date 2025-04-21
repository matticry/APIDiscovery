using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDiscovery.Models;
[Table("tbl_document_type")]
public class DocumentType
{
    [Key]
    public int id_d_t { get; set; }

    [MaxLength(250)]
    public string name_document { get; set; }
}