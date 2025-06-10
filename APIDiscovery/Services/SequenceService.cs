using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;
public class SequenceService : ISequenceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SequenceService> _logger;
    
    public SequenceService(ApplicationDbContext context, ILogger<SequenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseDto> CreateSequence(SequenceDto sequenceDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            // Verificar si el punto de emisión existe
            var emissionPointExists = await _context.EmissionPoints
                .AnyAsync(ep => ep.id_e_p == sequenceDto.IdEmissionPoint);
            
            if (!emissionPointExists)
            {
                response.Success = false;
                response.DisplayMessage = "El punto de emisión especificado no existe.";
                return response;
            }

            // Verificar si ya existe una secuencia con el mismo código para el mismo punto de emisión
            var existingSequence = await _context.Sequences
                .AnyAsync(s => s.code == sequenceDto.Code && s.id_emission_point == sequenceDto.IdEmissionPoint);
            
            if (existingSequence)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe una secuencia con el mismo código para este punto de emisión.";
                return response;
            }
            
            var newSequence = new Sequence
            {
                id_emission_point = sequenceDto.IdEmissionPoint,
                id_document_type = sequenceDto.IdDocumentType,
                code = sequenceDto.Code
            };
            
            await _context.Sequences.AddAsync(newSequence);
            await _context.SaveChangesAsync();
            
            response.Success = true;
            response.Result = newSequence;
            response.DisplayMessage = "Secuencia creada exitosamente.";
            
            _logger.LogInformation("Secuencia creada exitosamente con ID: {SequenceId}", newSequence.id_sequence);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al crear la secuencia");
            response.Success = false;
            response.DisplayMessage = "Error al crear la secuencia.";
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
            var sequence = await _context.Sequences
                .Include(s => s.EmissionPoint)
                .FirstOrDefaultAsync(s => s.id_sequence == id);

            if (sequence == null)
            {
                response.Success = false;
                response.DisplayMessage = "Secuencia no encontrada.";
                return response;
            }

            response.Success = true;
            response.Result = sequence;
            response.DisplayMessage = "Secuencia encontrada exitosamente.";
            
            _logger.LogInformation("Secuencia encontrada exitosamente con ID: {SequenceId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener la secuencia con ID: {SequenceId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al obtener la secuencia.";
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
            var sequences = await _context.Sequences
                .Include(s => s.EmissionPoint)
                .OrderBy(s => s.code)
                .ToListAsync();

            response.Success = true;
            response.Result = sequences;
            response.DisplayMessage = $"Se encontraron {sequences.Count} secuencias.";
            
            _logger.LogInformation("Consulta exitosa de todas las secuencias. Total: {Count}", sequences.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener todas las secuencias");
            response.Success = false;
            response.DisplayMessage = "Error al obtener las secuencias.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> GetByEmissionPointIdAsync(int emissionPointId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var sequences = await _context.Sequences
                .Include(s => s.EmissionPoint)
                .Where(s => s.id_emission_point == emissionPointId)
                .OrderBy(s => s.code)
                .ToListAsync();

            response.Success = true;
            response.Result = sequences;
            response.DisplayMessage = $"Se encontraron {sequences.Count} secuencias para el punto de emisión.";
            
            _logger.LogInformation("Consulta exitosa de secuencias por punto de emisión: {EmissionPointId}", emissionPointId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al obtener secuencias por punto de emisión: {EmissionPointId}", emissionPointId);
            response.Success = false;
            response.DisplayMessage = "Error al obtener las secuencias.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }

    public async Task<ResponseDto> UpdateAsync(int id, SequenceDto sequenceDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        try
        {
            var existingSequence = await _context.Sequences
                .FirstOrDefaultAsync(s => s.id_sequence == id);

            if (existingSequence == null)
            {
                response.Success = false;
                response.DisplayMessage = "Secuencia no encontrada.";
                return response;
            }

            // Verificar si el punto de emisión existe
            var emissionPointExists = await _context.EmissionPoints
                .AnyAsync(ep => ep.id_e_p == sequenceDto.IdEmissionPoint);
            
            if (!emissionPointExists)
            {
                response.Success = false;
                response.DisplayMessage = "El punto de emisión especificado no existe.";
                return response;
            }

            // Verificar si ya existe otra secuencia con el mismo código para el mismo punto de emisión
            var codeExists = await _context.Sequences
                .AnyAsync(s => s.code == sequenceDto.Code && 
                         s.id_emission_point == sequenceDto.IdEmissionPoint && 
                         s.id_sequence != id);

            if (codeExists)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe otra secuencia con el mismo código para este punto de emisión.";
                return response;
            }

            // Actualizar los campos
            existingSequence.id_emission_point = sequenceDto.IdEmissionPoint;
            existingSequence.id_document_type = sequenceDto.IdDocumentType;
            existingSequence.code = sequenceDto.Code;

            _context.Sequences.Update(existingSequence);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = existingSequence;
            response.DisplayMessage = "Secuencia actualizada exitosamente.";
            
            _logger.LogInformation("Secuencia actualizada exitosamente con ID: {SequenceId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al actualizar la secuencia con ID: {SequenceId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al actualizar la secuencia.";
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
            var sequence = await _context.Sequences
                .FirstOrDefaultAsync(s => s.id_sequence == id);

            if (sequence == null)
            {
                response.Success = false;
                response.DisplayMessage = "Secuencia no encontrada.";
                return response;
            }

            _context.Sequences.Remove(sequence);
            await _context.SaveChangesAsync();

            response.Success = true;
            response.Result = sequence;
            response.DisplayMessage = "Secuencia eliminada exitosamente.";
            
            _logger.LogInformation("Secuencia eliminada exitosamente con ID: {SequenceId}", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error al eliminar la secuencia con ID: {SequenceId}", id);
            response.Success = false;
            response.DisplayMessage = "Error al eliminar la secuencia.";
            response.ErrorMessages = [e.Message];
        }

        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return response;
    }
}