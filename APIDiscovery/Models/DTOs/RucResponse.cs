namespace APIDiscovery.Models.DTOs;

public class RucResponse
{
    public int StatusCode { get; set; }
    public string? TiempoRespuesta { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public RucInfo? Datos { get; set; }
    public string? IpSolicitante { get; set; }
}