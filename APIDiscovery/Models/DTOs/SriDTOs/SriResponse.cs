namespace APIDiscovery.Models.DTOs.SriDTOs;

public class SriResponse
{
    public string Estado { get; set; }
    public List<SriMessage> Mensajes { get; set; } = new List<SriMessage>();
    public string XmlResponse { get; set; } 
    
    public string RawResponse { get; set; }     // Respuesta XML cruda del SRI
}