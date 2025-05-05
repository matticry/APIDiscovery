namespace APIDiscovery.Models.DTOs.SriDTOs;

public class SriAutorizacionResponse
{
    public string ClaveAccesoConsultada { get; set; }
    public int NumeroComprobantes { get; set; }
    public List<SriAutorizacion> Autorizaciones { get; set; } = new List<SriAutorizacion>();
    public string RawResponse { get; set; }
    public string Error { get; set; }

}