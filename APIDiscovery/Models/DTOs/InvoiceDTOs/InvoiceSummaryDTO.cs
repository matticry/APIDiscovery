namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class InvoiceSummaryDTO
{
    public int InvoiceId { get; set; }
    public DateTime EmissionDate { get; set; }
    public string Status { get; set; }
    public string ElectronicStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string ClientName { get; set; }
    public string ClientDni { get; set; }
    public string SequenceNumber { get; set; }
}