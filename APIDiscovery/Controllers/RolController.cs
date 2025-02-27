using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolController : ControllerBase
{
    private readonly IRolService _rolService;
    
    public RolController(IRolService rolService)
    {
        _rolService = rolService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rol>>> GetAll()
    {
        return Ok(await _rolService.GetAllAsync());
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Rol>> GetById(int id)
    {
        try
        {
            return Ok(await _rolService.GetByIdAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Rol>> Create([FromBody] Rol rolRequest)
    {
        try
        {
            var newRol = await _rolService.CreateAsync(rolRequest);
            return CreatedAtAction(nameof(GetById), new { id = newRol.id_rol }, newRol);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Rol>> Update(int id, [FromBody] Rol rolRequest)
    {
        try
        {
            return Ok(await _rolService.UpdateAsync(id, rolRequest));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(int id)
    {
        try
        {
            return Ok(await _rolService.DeleteAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    
}