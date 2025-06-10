using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface IEnterpriseService
{
    
    Task<ResponseDto> CreateEnterprise(EnterpriseDto enterpriseDto);
    Task<ResponseDto> GetAllAsync();
    Task<ResponseDto> GetByIdAsync(int id);
    Task<ResponseDto> UpdateAsync(int id, EnterpriseDto enterpriseDto);
    Task<ResponseDto> DeleteAsync(int id);
    
    
}