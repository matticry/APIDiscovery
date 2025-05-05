namespace APIDiscovery.Models.DTOs;

public class LastSequenceInfoDto
{
    public int IdDocumentType { get; set; }
    public string DocumentTypeName { get; set; } // Optional: if you want to include document type name
    public string LastSequenceNumber { get; set; }
}