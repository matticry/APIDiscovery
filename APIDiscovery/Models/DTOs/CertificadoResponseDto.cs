namespace APIDiscovery.Models.DTOs;

public class CertificadoResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string RutaCertificado { get; set; }
    public DateTime? FechaExpiracion { get; set; }
}