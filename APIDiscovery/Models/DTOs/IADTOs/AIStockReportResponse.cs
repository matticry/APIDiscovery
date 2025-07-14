namespace APIDiscovery.Models.DTOs.IADTOs;

public class AIStockReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = "";
    public int TotalLowStockItems { get; set; }
    public List<LowStockItem> Items { get; set; } = new();
    public string AIRecommendation { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}