namespace APIDiscovery.Models.DTOs.IADTOs;

public class LowStockItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int CurrentStock { get; set; }
    public decimal UnitPrice { get; set; }
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public int RecommendedQuantity { get; set; }
    public string Priority { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public decimal EstimatedCost { get; set; }
}