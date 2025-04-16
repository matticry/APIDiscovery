namespace APIDiscovery.Models.DTOs;

public class FareDto
{
    public int Id { get; set; }
    public decimal Percentage { get; set; }
    
    public string Code { get; set; }
    public string Description { get; set; }
    public int IdTax { get; set; }
    public string TaxDescription { get; set; }
}