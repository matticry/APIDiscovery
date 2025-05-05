using System.Security.Cryptography.X509Certificates;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Utils;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class CertificadoService : ICertificadoService
{
    private readonly string _certificadosPath;
    private readonly IConfiguration _configuration;

    private readonly ApplicationDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<CertificadoService> _logger;

    public CertificadoService(
        ApplicationDbContext context,
        IConfiguration configuration,
        EncryptionHelper encryptionHelper,
        ILogger<CertificadoService> logger)
    {
        _context = context;
        _configuration = configuration;
        _encryptionHelper = encryptionHelper;
        _logger = logger;

        // Crear carpeta de certificados si no existe
        _certificadosPath = Path.Combine(Directory.GetCurrentDirectory(), "Certificados");
        if (!Directory.Exists(_certificadosPath))
            Directory.CreateDirectory(_certificadosPath);
    }


    public async Task<CertificadoResponseDto> UploadCertificado(IFormFile archivo, string ruc, string clave)
    {
        var response = new CertificadoResponseDto { Success = false };

        try
        {
            if (archivo == null || archivo.Length == 0)
            {
                response.Message = "No se ha proporcionado un archivo válido";
                return response;
            }

            // Validar formato del archivo
            if (!archivo.FileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
            {
                response.Message = "El archivo debe tener formato .p12";
                return response;
            }

            // Validar el certificado con la clave
            DateTime fechaExpiracion;
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await archivo.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var cert = new X509Certificate2(memoryStream.ToArray(), clave,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                    fechaExpiracion = cert.NotAfter;

                    if (DateTime.Now > fechaExpiracion)
                    {
                        response.Message =
                            $"El certificado está expirado. Fecha de expiración: {fechaExpiracion:dd/MM/yyyy}";
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el certificado");
                response.Message = "El certificado es inválido o la contraseña es incorrecta";
                return response;
            }

            // Guardar el archivo
            var nombreArchivo = $"cert_{ruc}_{DateTime.Now:yyyyMMddHHmmss}.p12";
            var rutaCompleta = Path.Combine(_certificadosPath, nombreArchivo);

            using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            // Guardar en base de datos
            var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
            if (empresa == null)
            {
                response.Message = $"No se encontró empresa con RUC {ruc}";
                // Eliminar el archivo si no se puede asociar a una empresa
                File.Delete(rutaCompleta);
                return response;
            }

            // Encriptar clave
            var claveEncriptada = _encryptionHelper.Encrypt(clave);

            // Actualizar información de la empresa
            empresa.electronic_signature = nombreArchivo;
            empresa.key_signature = claveEncriptada;
            empresa.start_date_signature = DateTime.Now;
            empresa.end_date_signature = fechaExpiracion;

            await _context.SaveChangesAsync();

            response.Success = true;
            response.Message = "Certificado cargado correctamente";
            response.RutaCertificado = nombreArchivo;
            response.FechaExpiracion = fechaExpiracion;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UploadCertificado");
            response.Message = $"Error al procesar el certificado: {ex.Message}";
            return response;
        }
    }

    public async Task<bool> ValidarCertificado(string ruc)
    {
        try
        {
            var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
            if (empresa == null || string.IsNullOrEmpty(empresa.electronic_signature))
                return false;

            var rutaCertificado = Path.Combine(_certificadosPath, empresa.electronic_signature);
            if (!File.Exists(rutaCertificado))
                return false;

            // Validar fechas
            if (!empresa.start_date_signature.HasValue || !empresa.end_date_signature.HasValue)
                return false;

            if (DateTime.Now < empresa.start_date_signature || DateTime.Now > empresa.end_date_signature)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ValidarCertificado");
            return false;
        }
    }

    public async Task<string> ObtenerCertificadoPath(string ruc)
    {
        var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
        if (empresa == null || string.IsNullOrEmpty(empresa.electronic_signature))
            return null;

        return Path.Combine(_certificadosPath, empresa.electronic_signature);
    }

    public async Task<string> ObtenerClaveDesencriptada(string ruc)
    {
        var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
        if (empresa == null || string.IsNullOrEmpty(empresa.key_signature))
            return null;

        return _encryptionHelper.Decrypt(empresa.key_signature);
    }
}