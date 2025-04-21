using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface IEmissionPointService
{
    Task<List<EmissionPointWithSequenceDto>> GetEmissionPointsWithSequencesByBranchId(int branchId);
}