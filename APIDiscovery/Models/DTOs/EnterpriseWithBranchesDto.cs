namespace APIDiscovery.Models.DTOs;

public class EnterpriseWithBranchesDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public string ComercialName { get; set; }
    public string Ruc { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Logo { get; set; }
    public List<BranchDto> Branches { get; set; }
}