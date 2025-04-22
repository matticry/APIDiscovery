namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class InvoicePaymentDTO
{
    public decimal Total { get; set; }
    public int Deadline { get; set; }
    public string UnitTime { get; set; }
    public string PaymentDetail { get; set; }
}