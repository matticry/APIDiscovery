using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class EmissionPointService : IEmissionPointService
{
    
    private readonly ApplicationDbContext _context;

    public EmissionPointService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<List<EmissionPointWithSequenceDto>> GetEmissionPointsWithSequencesByBranchId(int branchId)
    {
        var emissionPoints = await _context.EmissionPoints
            .Where(ep => ep.id_branch == branchId)
            .Include(ep => ep.Sequences)
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
            }).ToList()
        }).ToList();

        return result;
    }
}
