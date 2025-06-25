using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class EmissionPointService : IEmissionPointService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmissionPointService> _logger;
    
    public EmissionPointService(ApplicationDbContext context, ILogger<EmissionPointService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<EmissionPointWithSequenceDto>> GetEmissionPointsWithSequencesByBranchId(int branchId)
    {
        var emissionPoints = await _context.EmissionPoints
            .Where(ep => ep.id_branch == branchId)
            .Include(ep => ep.Sequences)
            .ToListAsync();

        // Get the last invoices/documents for each emission point and document type
        var lastInvoices = await _context.Invoices 
            .Where(i => i.branch_id == branchId)
            .GroupBy(i => new { i.id_emission_point, i.receipt_id })
            .Select(g => new
            {
                EmissionPointId = g.Key.id_emission_point,
                DocumentTypeId = g.Key.receipt_id,
                LastSequenceNumber = g.Max(i => i.sequence), 
            })
            .ToListAsync();

        var result = emissionPoints.Select(ep => new EmissionPointWithSequenceDto
        {
            IdEmissionPoint = ep.id_e_p,
            Code = ep.code,
            Details = ep.details,
            Type = ep.type,
            Sequences = ep.Sequences.Select(s => new SequenceDto
            {
                IdSequence = s.id_sequence,
                IdDocumentType = s.id_document_type,
                Code = s.code
            }).ToList(),
            LastSequences = lastInvoices
                .Where(li => li.EmissionPointId == ep.id_e_p)
                .Select(li => new LastSequenceInfoDto
                {
                    IdDocumentType = li.DocumentTypeId,
                    DocumentTypeName = li.DocumentTypeId == 1 ? "Factura" : "Boleta",
                    LastSequenceNumber = li.LastSequenceNumber
                }).ToList()
        }).ToList();

        return result;
    }

    public async Task<ResponseDto> CreateEmissionPoint(EmissionPointDto emissionPointDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            // Verificar si la sucursal existe
            var branchExists = await _context.Branches
                .AnyAsync(b => b.id_br == emissionPointDto.IdBranch);
            
            if (!branchExists)
            {
                response.Success = false;
                response.DisplayMessage = "La sucursal especificada no existe.";
                return response;
            }

            // Verificar si ya existe un punto de emisión con el mismo código para la misma sucursal
            var existingEmissionPoint = await _context.EmissionPoints
                .AnyAsync(ep => ep.code == emissionPointDto.Code && ep.id_branch == emissionPointDto.IdBranch);
            
            if (existingEmissionPoint)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe un punto de emisión con el mismo código para esta sucursal.";
                return response;
            }
            
            var newEmissionPoint = new EmissionPoint
            {
                code = emissionPointDto.Code,
                details = emissionPointDto.Details,
                type = emissionPointDto.Type,
                id_branch = emissionPointDto.IdBranch
            };
            
            await _context.EmissionPoints.AddAsync(newEmissionPoint);
            await _context.SaveChangesAsync();
            
            response.Success = true;
            response.Result = newEmissionPoint;
            response.DisplayMessage = "Punto de emisión creado exitosamente.";
            
            _logger.LogInformation("Punto de emisión creado exitosamente con ID: {EmissionPointId}", newEmissionPoint.id_e_p);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al crear el punto de emisión");
            response.Success = false;
            response.DisplayMessage = "Error al crear el punto de emisión.";
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
            var emissionPoint = await _context.EmissionPoints
                .Include(ep => ep.Branch)
                .Include(ep => ep.Sequences)
                .FirstOrDefaultAsync(ep => ep.id_e_p == id);

            if (emissionPoint == null)
            {
                response.Success = false;
                response.DisplayMessage = "Punto de emisión no encontrado.";
                return response;
            }

            response.Success = true;
            response.Result = emissionPoint;
            response.DisplayMessage = "Punto de emisión encontrado exitosamente.";
            
            _logger.LogInformation("Punto de emisión encontrado exitosamente con ID: {EmissionPointId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener el punto de emisión con ID: {EmissionPointId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al obtener el punto de emisión.";
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
            var emissionPoints = await _context.EmissionPoints
                .Include(ep => ep.Branch)
                .Include(ep => ep.Sequences)
                .OrderBy(ep => ep.code)
                .ToListAsync();

            response.Success = true;
            response.Result = emissionPoints;
            response.DisplayMessage = $"Se encontraron {emissionPoints.Count} puntos de emisión.";
            
            _logger.LogInformation("Consulta exitosa de todos los puntos de emisión. Total: {Count}", emissionPoints.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener todos los puntos de emisión");
            response.Success = false;
            response.DisplayMessage = "Error al obtener los puntos de emisión.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> GetByBranchIdAsync(int branchId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var emissionPoints = await _context.EmissionPoints
                .Include(ep => ep.Branch)
                .Include(ep => ep.Sequences)
                .Where(ep => ep.id_branch == branchId)
                .OrderBy(ep => ep.code)
                .ToListAsync();

            response.Success = true;
            response.Result = emissionPoints;
            response.DisplayMessage = $"Se encontraron {emissionPoints.Count} puntos de emisión para la sucursal.";
            
            _logger.LogInformation("Consulta exitosa de puntos de emisión por sucursal: {BranchId}", branchId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener puntos de emisión por sucursal: {BranchId}", branchId);
            response.Success = false;
            response.DisplayMessage = "Error al obtener los puntos de emisión.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> UpdateAsync(int id, EmissionPointDto emissionPointDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var existingEmissionPoint = await _context.EmissionPoints
                .FirstOrDefaultAsync(ep => ep.id_e_p == id);

            if (existingEmissionPoint == null)
            {
                response.Success = false;
                response.DisplayMessage = "Punto de emisión no encontrado.";
                return response;
            }

            // Verificar si la sucursal existe
            var branchExists = await _context.Branches
                .AnyAsync(b => b.id_br == emissionPointDto.IdBranch);
            
            if (!branchExists)
            {
                response.Success = false;
                response.DisplayMessage = "La sucursal especificada no existe.";
                return response;
            }

            // Verificar si ya existe otro punto de emisión con el mismo código para la misma sucursal
            var codeExists = await _context.EmissionPoints
                .AnyAsync(ep => ep.code == emissionPointDto.Code && 
                         ep.id_branch == emissionPointDto.IdBranch && 
                         ep.id_e_p != id);

            if (codeExists)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe otro punto de emisión con el mismo código para esta sucursal.";
                return response;
            }

            // Actualizar los campos
            existingEmissionPoint.code = emissionPointDto.Code;
            existingEmissionPoint.details = emissionPointDto.Details;
            existingEmissionPoint.type = emissionPointDto.Type;
            existingEmissionPoint.id_branch = emissionPointDto.IdBranch;

            _context.EmissionPoints.Update(existingEmissionPoint);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = existingEmissionPoint;
            response.DisplayMessage = "Punto de emisión actualizado exitosamente.";
            
            _logger.LogInformation("Punto de emisión actualizado exitosamente con ID: {EmissionPointId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al actualizar el punto de emisión con ID: {EmissionPointId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al actualizar el punto de emisión.";
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
            var emissionPoint = await _context.EmissionPoints
                .Include(ep => ep.Sequences)
                .FirstOrDefaultAsync(ep => ep.id_e_p == id);

            if (emissionPoint == null)
            {
                response.Success = false;
                response.DisplayMessage = "Punto de emisión no encontrado.";
                return response;
            }

            // Verificar si tiene secuencias asociadas
            if (emissionPoint.Sequences != null && emissionPoint.Sequences.Count != 0)
            {
                response.Success = false;
                response.DisplayMessage = "No se puede eliminar el punto de emisión porque tiene secuencias asociadas.";
                return response;
            }

            _context.EmissionPoints.Remove(emissionPoint);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = emissionPoint;
            response.DisplayMessage = "Punto de emisión eliminado exitosamente.";
            
            _logger.LogInformation("Punto de emisión eliminado exitosamente con ID: {EmissionPointId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al eliminar el punto de emisión con ID: {EmissionPointId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al eliminar el punto de emisión.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }
}