using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services.Commands;

public class CustomService
{
    protected ApplicationDbContext _context;

    public CustomService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ResponseDto> GetTaxRateByCode(string taxCode)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
    
        try
        {
            var taxRate = await _context.Fares
                .Where(f => f.code == taxCode)
                .Select(f => new
                {
                    Id = f.id_fare,
                    Percentage = f.percentage,
                    Description = f.description
                })
                .FirstOrDefaultAsync();
            
            if (taxRate == null)
            {
                response.Success = false;
                response.DisplayMessage = "No se encontró la tarifa con el código especificado.";
                return response;
            }
        
            response.Result = taxRate;
            response.DisplayMessage = "Tarifa obtenida exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener la tarifa.";
            response.ErrorMessages = [ex.Message];
        }
    
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
    
        return response;
    }
    
    

    public async Task<IEnumerable<string>> GetResultRoleByNameAsync()
    {
        return await _context.Roles
            .Where(r => EF.Functions.Like(r.name_rol, "%Enfermero/a%") || EF.Functions.Like(r.name_rol, "%Familiar%"))
            .Select(r => r.name_rol)
            .ToListAsync();
    }
    
    public async Task<ResponseDto> GetUserSubscriptionDates(int enterpriseId, int userId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
    
        try
        {
            var subscription = await _context.EnterpriseUsers
                .Where(eu => eu.id_enterprise == enterpriseId && eu.id_user == userId)
                .Select(eu => new SubscriptionDatesDto
                {
                    StartDate = eu.start_date_subscription,
                    EndDate = eu.end_date_subscription,
                    Status = eu.status
                })
                .FirstOrDefaultAsync();
            
            if (subscription == null)
            {
                response.Success = false;
                response.DisplayMessage = "No se encontró una relación entre el usuario y la empresa especificados.";
                return response;
            }
        
            response.Result = subscription;
            response.DisplayMessage = "Fechas de suscripción obtenidas exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener las fechas de suscripción.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
    
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
    
        return response;
    }
    
    

    public async Task<UserEnterprisesDto> GetUserEnterprisesAndBranches(int userId)
    {
        var user = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.id_us == userId);

        if (user == null) throw new NotFoundException("Usuario no encontrado.");

        var enterpriseUsers = await _context.EnterpriseUsers
            .Where(eu => eu.id_user == userId && eu.status == 'A')
            .ToListAsync();

        var userEnterprises = new List<EnterpriseWithBranchesDto>();

        foreach (var eu in enterpriseUsers)
        {
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.id_en == eu.id_enterprise);

            if (enterprise == null) continue;
            var branches = await _context.Branches
                .Where(b => b.id_enterprise == enterprise.id_en && b.status == 'A')
                .Select(b => new BranchDto
                {
                    Id = b.id_br,
                    Code = b.code,
                    Description = b.description,
                    Address = b.address,
                    Phone = b.phone
                })
                .ToListAsync();

            userEnterprises.Add(new EnterpriseWithBranchesDto
            {
                Id = enterprise.id_en,
                CompanyName = enterprise.company_name,
                ComercialName = enterprise.comercial_name,
                Ruc = enterprise.ruc,
                Address = enterprise.address_matriz,
                Phone = enterprise.phone,
                Email = enterprise.email,
                Logo = enterprise.logo,
                Branches = branches
            });
        }

        return new UserEnterprisesDto
        {
            UserId = user.id_us,
            UserName = $"{user.name_us} {user.lastname_us}",
            Enterprises = userEnterprises
        };
    }
    
    public async Task<ResponseDto> GetCategoriesByEnterprise(int enterpriseId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var categories = await _context.Categories
                .Where(c => c.id_enterprise == enterpriseId && c.status == 'A')
                .Select(c => new
                {
                    Id = c.id_ca,
                    Name = c.name,
                    Description = c.description
                })
                .ToListAsync();
                
            response.Result = categories;
            response.DisplayMessage = "Categorías obtenidas exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener las categorías.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
}