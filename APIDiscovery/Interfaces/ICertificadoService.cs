using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface ICertificadoService
{
    
    Task<CertificadoResponseDto> UploadCertificado(IFormFile archivo, string ruc, string clave);
    Task<bool> ValidarCertificado(string ruc);
    Task<string> ObtenerCertificadoPath(string ruc);
    Task<string> ObtenerClaveDesencriptada(string ruc);
    
}