using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetAllClients()
    {
        var clients = await _clientService.GetAllAsync();
        return Ok(clients);
    }
    
    [HttpGet("enterprise/{enterpriseId}")]
    public async Task<ActionResult<IEnumerable<Client>>> GetClientsByEnterprise(int enterpriseId)
    {
        var clients = await _clientService.GetAllAsync(enterpriseId);
        return Ok(clients);
    }
    
    [HttpGet("GetTotalClientsActivesAsync/{enterpriseId}")]
    public async Task<ActionResult<int>> GetTotalClientsActivesAsync(int enterpriseId)
    {
        var total = await _clientService.GetTotalClientsActivesAsync(enterpriseId);
        return Ok(total);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> GetClientById(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return Ok(client);
    }
    
    [HttpGet("dni/{dni}")]
    public async Task<ActionResult<Client>> GetClientByDni(string dni)
    {
        var client = await _clientService.GetByDniAsync(dni);
        return Ok(client);
    }
    
    [HttpGet("{id}/enterprises")]
    public async Task<ActionResult<IEnumerable<Enterprise>>> GetClientEnterprises(int id)
    {
        var enterprises = await _clientService.GetClientEnterprisesAsync(id);
        return Ok(enterprises);
    }
    
    [HttpPost("enterprise/{enterpriseId}")]
    public async Task<ActionResult<Client>> CreateClient([FromBody] Client client, int enterpriseId)
    {
        var createdClient = await _clientService.CreateAsync(client, enterpriseId);
        return CreatedAtAction(nameof(GetClientById), new { id = createdClient.id_client }, createdClient);
    }
    
    [HttpPost("{clientId}/enterprise/{enterpriseId}")]
    public async Task<ActionResult> AssignClientToEnterprise(int clientId, int enterpriseId)
    {
        await _clientService.AssignClientToEnterpriseAsync(clientId, enterpriseId);
        return NoContent();
    }
    [HttpPut("DesactiveClientAsync/{clientId}/{enterpriseId}")]
    public async Task<ActionResult<ResponseDto>> DesactiveClientAsync(int clientId, int enterpriseId)
    {
        var response = await _clientService.DesactiveClientAsync(clientId, enterpriseId);
        return Ok(response);
    }
    
    [HttpPut("ActiveClientAsync/{clientId}/{enterpriseId}")]
    public async Task<ActionResult<ResponseDto>> ActiveClientAsync(int clientId, int enterpriseId)
    {
        var response = await _clientService.ActiveClientAsync(clientId, enterpriseId);
        return Ok(response);
    }
    
    [HttpPut("{id}/enterprise/{enterpriseId}")]
    public async Task<ActionResult<Client>> UpdateClient(int id, [FromBody] Client client, int enterpriseId)
    {
        var updatedClient = await _clientService.UpdateAsync(id, client, enterpriseId);
        return Ok(updatedClient);
    }
    
    
    
    [HttpDelete("{clientId}/enterprise/{enterpriseId}")]
    public async Task<ActionResult> RemoveClientFromEnterprise(int clientId, int enterpriseId)
    {
        await _clientService.RemoveClientFromEnterpriseAsync(clientId, enterpriseId);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(int id)
    {
        await _clientService.DeleteAsync(id);
        return NoContent();
    }
    
    
}