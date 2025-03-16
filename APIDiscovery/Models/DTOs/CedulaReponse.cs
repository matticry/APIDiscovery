namespace APIDiscovery.Models.DTOs;

public class CedulaResponse
{
    public int StatusCode { get; set; }
    public string TiempoRespuesta { get; set; }
    public CedulaInfo Datos { get; set; }
    public string Error { get; set; }
    public string Message { get; set; } 
    public string VersionApi { get; set; } = "beta - v2025.03.13";
    public string IpSolicitante { get; set; }

}