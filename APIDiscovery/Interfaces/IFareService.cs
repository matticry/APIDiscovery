using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface IFareService
{
    Task<ResponseDto> GetAllFares();
    
}