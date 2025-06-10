using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EnterpriseController : ControllerBase
{
    private readonly IEnterpriseService _enterpriseService;
    
    public EnterpriseController(IEnterpriseService enterpriseService)
    {
        _enterpriseService = enterpriseService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateEnterprise([FromBody] EnterpriseDto enterpriseDto)
    {
        var response = await _enterpriseService.CreateEnterprise(enterpriseDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnterpriseById(int id)
    {
        var response = await _enterpriseService.GetByIdAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllEnterprises()
    {
        var response = await _enterpriseService.GetAllAsync();
        return Ok(response);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEnterprise(int id, [FromBody] EnterpriseDto enterpriseDto)
    {
        var response = await _enterpriseService.UpdateAsync(id, enterpriseDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEnterprise(int id)
    {
        var response = await _enterpriseService.DeleteAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
}