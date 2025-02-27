using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmpresaController : ControllerBase
{

    private readonly IEmpresaService _empresaService;
    
    public EmpresaController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empresa>>> GetAll()
    {
        return Ok(await _empresaService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Empresa>> GetById(int id)
    {
        try
        {
            return Ok(await _empresaService.GetByIdAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Empresa>> Create([FromBody] Empresa empresaRequest)
    {
        try
        {
            var newEmpresa = await _empresaService.CreateAsync(empresaRequest);
            return CreatedAtAction(nameof(GetById), new { id = newEmpresa.id_empresa }, newEmpresa);
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
    public async Task<ActionResult<Empresa>> Update(int id, [FromBody] Empresa empresaRequest)
    {
        try
        {
            return Ok(await _empresaService.UpdateAsync(id, empresaRequest));
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
            return Ok(await _empresaService.DeleteAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
        
    
}