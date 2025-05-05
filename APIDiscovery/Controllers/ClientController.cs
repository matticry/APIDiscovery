using APIDiscovery.Interfaces;
using APIDiscovery.Models;
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
    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _clientService.GetAllAsync();
    }
    
    [HttpGet("{id:int}")] 
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return Ok(client);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] Client client)
    {
        var createdClient = await _clientService.CreateAsync(client);
        return Ok(createdClient);
    }
    
    [HttpPut("{id:int}")] 
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] Client client)
    {
        var updatedClient = await _clientService.UpdateAsync(id, client);
        return Ok(updatedClient);
    }
    
    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var result = await _clientService.DeleteAsync(id);
        return Ok(result);
    }
}