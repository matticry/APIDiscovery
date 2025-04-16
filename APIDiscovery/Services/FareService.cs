using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class FareService : IFareService
{
    private readonly ApplicationDbContext _context;
    
    public FareService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ResponseDto> GetAllFares()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var fares = await _context.Fares
                .Include(f => f.Tax)
                .Select(f => new FareDto
                {
                    Id = f.id_fare,
                    Percentage = f.percentage,
                    Code = f.code,
                    Description = f.description,
                    IdTax = f.id_tax,
                    TaxDescription = f.Tax.description
                })
                .ToListAsync();
                
            response.Result = fares;
            response.DisplayMessage = "Tarifas obtenidas exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener las tarifas.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
}