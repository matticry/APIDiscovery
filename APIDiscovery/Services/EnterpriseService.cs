using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class EnterpriseService : IEnterpriseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EnterpriseService> _logger;
    
    public EnterpriseService(ApplicationDbContext context, ILogger<EnterpriseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseDto> CreateEnterprise(EnterpriseDto enterpriseDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var existingEnterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == enterpriseDto.Ruc);
            if (existingEnterprise != null)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe una empresa con el mismo RUC.";
                return response;
            }
            
            var newEnterprise = new Enterprise()
            {
                comercial_name = enterpriseDto.ComercialName,
                company_name = enterpriseDto.CompanyName,
                ruc = enterpriseDto.Ruc,
                address_matriz = enterpriseDto.AddressMatriz,
                email = enterpriseDto.Email,
                phone = enterpriseDto.Phone,
                special_taxpayer = enterpriseDto.SpecialTaxpayer,
                accountant = enterpriseDto.Accountant,
                email_user = enterpriseDto.EmailUser,
                email_password = enterpriseDto.EmailPassword,
                email_port = enterpriseDto.EmailPort,
                email_smtp = enterpriseDto.EmailSmtp,
                email_security = enterpriseDto.EmailSecurity,
                logo = enterpriseDto.Logo,
                retention_agent = enterpriseDto.RetentionAgent,
                environment = enterpriseDto.Environment
            };
            
            await _context.Enterprises.AddAsync(newEnterprise);
            await _context.SaveChangesAsync();
            
            response.Success = true;
            response.Result = newEnterprise;
            response.DisplayMessage = "Empresa creada exitosamente.";
            
            _logger.LogInformation("Empresa creada exitosamente con ID: {EnterpriseId}", newEnterprise.id_en);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al crear la empresa con RUC: {Ruc}", enterpriseDto.Ruc);
            response.Success = false;
            response.DisplayMessage = "Error al crear la empresa.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> GetAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var enterprises = await _context.Enterprises
                .OrderBy(e => e.company_name)
                .ToListAsync();

            response.Success = true;
            response.Result = enterprises;
            response.DisplayMessage = $"Se encontraron {enterprises.Count} empresas.";
            
            _logger.LogInformation("Consulta exitosa de todas las empresas. Total: {Count}", enterprises.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener todas las empresas");
            response.Success = false;
            response.DisplayMessage = "Error al obtener las empresas.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> GetByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.id_en == id);

            if (enterprise == null)
            {
                response.Success = false;
                response.DisplayMessage = "Empresa no encontrada.";
                return response;
            }

            response.Success = true;
            response.Result = enterprise;
            response.DisplayMessage = "Empresa encontrada exitosamente.";
            
            _logger.LogInformation("Empresa encontrada exitosamente con ID: {EnterpriseId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener la empresa con ID: {EnterpriseId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al obtener la empresa.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> UpdateAsync(int id, EnterpriseDto enterpriseDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var existingEnterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.id_en == id);

            if (existingEnterprise == null)
            {
                response.Success = false;
                response.DisplayMessage = "Empresa no encontrada.";
                return response;
            }

            // Verificar si el RUC ya existe en otra empresa
            var rucExists = await _context.Enterprises
                .AnyAsync(e => e.ruc == enterpriseDto.Ruc && e.id_en != id);

            if (rucExists)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe otra empresa con el mismo RUC.";
                return response;
            }

            existingEnterprise.comercial_name = enterpriseDto.ComercialName;
            existingEnterprise.company_name = enterpriseDto.CompanyName;
            existingEnterprise.ruc = enterpriseDto.Ruc;
            existingEnterprise.address_matriz = enterpriseDto.AddressMatriz;
            existingEnterprise.email = enterpriseDto.Email;
            existingEnterprise.phone = enterpriseDto.Phone;
            existingEnterprise.special_taxpayer = enterpriseDto.SpecialTaxpayer;
            existingEnterprise.accountant = enterpriseDto.Accountant;
            existingEnterprise.email_user = enterpriseDto.EmailUser;
            existingEnterprise.email_password = enterpriseDto.EmailPassword;
            existingEnterprise.email_port = enterpriseDto.EmailPort;
            existingEnterprise.email_smtp = enterpriseDto.EmailSmtp;
            existingEnterprise.email_security = enterpriseDto.EmailSecurity;
            existingEnterprise.logo = enterpriseDto.Logo;
            existingEnterprise.retention_agent = enterpriseDto.RetentionAgent;
            existingEnterprise.environment = enterpriseDto.Environment;

            _context.Enterprises.Update(existingEnterprise);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = existingEnterprise;
            response.DisplayMessage = "Empresa actualizada exitosamente.";
            
            _logger.LogInformation("Empresa actualizada exitosamente con ID: {EnterpriseId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al actualizar la empresa con ID: {EnterpriseId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al actualizar la empresa.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> DeleteAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.id_en == id);

            if (enterprise == null)
            {
                response.Success = false;
                response.DisplayMessage = "Empresa no encontrada.";
                return response;
            }

            var hasUsers = await _context.Set<EnterpriseUser>()
                .AnyAsync(eu => eu.id_e_u == id);

            if (hasUsers)
            {
                response.Success = false;
                response.DisplayMessage = "No se puede eliminar la empresa porque tiene usuarios asociados.";
                return response;
            }

            _context.Enterprises.Remove(enterprise);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = enterprise;
            response.DisplayMessage = "Empresa eliminada exitosamente.";
            
            _logger.LogInformation("Empresa eliminada exitosamente con ID: {EnterpriseId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al eliminar la empresa con ID: {EnterpriseId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al eliminar la empresa.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }
}