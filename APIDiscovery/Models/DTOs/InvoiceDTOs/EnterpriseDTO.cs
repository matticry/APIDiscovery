namespace APIDiscovery.Models.DTOs.InvoiceDTOs;

public class EnterpriseDTO
{
    public int IdEnterprise { get; set; }
    public string? CompanyName { get; set; }
    public string? ComercialName { get; set; }
    public string? Ruc { get; set; }
    public string? AddressMatriz { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public char Accountant { get; set; }
    public int? Enviroment { get; set; }
}