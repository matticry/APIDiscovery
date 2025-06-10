using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SequenceController : ControllerBase
{
    private readonly ISequenceService _sequenceService;
    
    public SequenceController(ISequenceService sequenceService)
    {
        _sequenceService = sequenceService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateSequence([FromBody] SequenceDto sequenceDto)
    {
        var response = await _sequenceService.CreateSequence(sequenceDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSequenceById(int id)
    {
        var response = await _sequenceService.GetByIdAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllSequences()
    {
        var response = await _sequenceService.GetAllAsync();
        return Ok(response);
    }
    
    [HttpGet("emissionPoint/{emissionPointId}")]
    public async Task<IActionResult> GetSequencesByEmissionPoint(int emissionPointId)
    {
        var response = await _sequenceService.GetByEmissionPointIdAsync(emissionPointId);
        return Ok(response);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSequence(int id, [FromBody] SequenceDto sequenceDto)
    {
        var response = await _sequenceService.UpdateAsync(id, sequenceDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSequence(int id)
    {
        var response = await _sequenceService.DeleteAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
}