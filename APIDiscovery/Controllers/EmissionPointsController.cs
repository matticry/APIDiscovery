using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmissionPointsController : ControllerBase
{
    private readonly IEmissionPointService _emissionPointService;

    public EmissionPointsController(IEmissionPointService emissionPointService)
    {
        _emissionPointService = emissionPointService;
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<List<EmissionPointWithSequenceDto>>> GetByBranch(int branchId)
    {
        var emissionPoints = await _emissionPointService.GetEmissionPointsWithSequencesByBranchId(branchId);
        if (emissionPoints.Count == 0)
            return NotFound($"No se encontraron puntos de emisión para la sucursal {branchId}");

        return Ok(emissionPoints);
    }
    [HttpPost]
    public async Task<IActionResult> CreateEmissionPoint([FromBody] EmissionPointDto emissionPointDto)
    {
        var response = await _emissionPointService.CreateEmissionPoint(emissionPointDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmissionPointById(int id)
    {
        var response = await _emissionPointService.GetByIdAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllEmissionPoints()
    {
        var response = await _emissionPointService.GetAllAsync();
        return Ok(response);
    }
    
    [HttpGet("GetEmissionPointsByBranch/{branchId}")]
    public async Task<IActionResult> GetEmissionPointsByBranch(int branchId)
    {
        var response = await _emissionPointService.GetByBranchIdAsync(branchId);
        return Ok(response);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmissionPoint(int id, [FromBody] EmissionPointDto emissionPointDto)
    {
        var response = await _emissionPointService.UpdateAsync(id, emissionPointDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmissionPoint(int id)
    {
        var response = await _emissionPointService.DeleteAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }

}