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

        // Get the last invoices/documents for each emission point and document type
        var lastInvoices = await _context.Invoices // Assuming you have an Invoices table
            .Where(i => i.branch_id == branchId)
            .GroupBy(i => new { i.id_emission_point, i.receipt_id })
            .Select(g => new
            {
                EmissionPointId = g.Key.id_emission_point,
                DocumentTypeId = g.Key.receipt_id,
                LastSequenceNumber = g.Max(i => i.sequence), // Assuming you have this field
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
    

}
