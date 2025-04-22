namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class InvoiceDetailDTO
{
    public string CodeStub { get; set; }
    public string Description { get; set; }
    public int Amount { get; set; }
    public decimal PriceUnit { get; set; }
    public decimal PriceWithDiscount { get; set; }
    public decimal Neto { get; set; }
    public decimal IvaPorc { get; set; }
    public decimal IvaValor { get; set; }
    public decimal IcePorc { get; set; }
    public decimal IceValor { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Note1 { get; set; }
    public string Note2 { get; set; }
    public string Note3 { get; set; }
}