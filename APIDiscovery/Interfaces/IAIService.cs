using APIDiscovery.Models.DTOs.IADTOs;

namespace APIDiscovery.Interfaces;

public interface IAiService
{
    
    Task<AIStockReportResponse> GetLowStockReportAsync(int enterpriseId);

    
}