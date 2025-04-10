namespace APIDiscovery.Models.DTOs;

public class UserEnterprisesDto
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public List<EnterpriseWithBranchesDto> Enterprises { get; set; }
}