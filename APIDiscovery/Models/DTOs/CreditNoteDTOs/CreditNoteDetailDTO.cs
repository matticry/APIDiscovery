namespace APIDiscovery.Models.DTOs.CreditNoteDTOs;

public class CreditNoteDetailDTO
{
    public int IdCreditNoteDetail { get; set; }
    public string CodeStub { get; set; }
    public string Description { get; set; }
    public int Amount { get; set; }
    public decimal PriceUnit { get; set; }
    public decimal Discount { get; set; }
    public decimal Neto { get; set; }
    public decimal IvaPorc { get; set; }
    public decimal IcePorc { get; set; }
    public decimal IvaValor { get; set; }
    public decimal IceValor { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Nota1 { get; set; }
    public string Nota2 { get; set; }
    public string Nota3 { get; set; }
    public ArticleDto Article { get; set; }
    public FareDto Tariff { get; set; }
}