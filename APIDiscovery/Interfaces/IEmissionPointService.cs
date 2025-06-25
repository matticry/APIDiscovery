using APIDiscovery.Models.DTOs;
using APIDiscovery.Models.DTOs.InvoiceDTOs;

namespace APIDiscovery.Interfaces;

public interface IEmissionPointService
{
    Task<List<EmissionPointWithSequenceDto>> GetEmissionPointsWithSequencesByBranchId(int branchId);
    Task<ResponseDto> CreateEmissionPoint(EmissionPointDto emissionPointDto);
    Task<ResponseDto> GetByIdAsync(int id);
    Task<ResponseDto> GetAllAsync();
    Task<ResponseDto> GetByBranchIdAsync(int branchId);
    Task<ResponseDto> UpdateAsync(int id, EmissionPointDto emissionPointDto);
    Task<ResponseDto> DeleteAsync(int id);
}