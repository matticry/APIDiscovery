namespace APIDiscovery.Models.DTOs;

public class ArticleDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public decimal PriceUnit { get; set; }
    public int Stock { get; set; }
    public char? Type { get; set; } 
    public char? IncludeVat { get; set; } 
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public string? Image { get; set; }
    public string Description { get; set; }
    public int IdEnterprise { get; set; }
    public int IdCategory { get; set; }
    public string CategoryName { get; set; }
    public List<FareDto> Fares { get; set; }
    public string StockStatus { get; set; }
    public string StockMessage { get; set; }
}