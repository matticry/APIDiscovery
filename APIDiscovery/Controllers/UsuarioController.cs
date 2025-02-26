using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Route("api/usuarios")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Usuario>>> GetAll()
    {
        return Ok(await _usuarioService.GetAllAsync());
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<Usuario>> GetById(int id)
    {
        try
        {
            return Ok(await _usuarioService.GetByIdAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<Usuario>> GetByEmail(string email)
    {
        try
        {
            return Ok(await _usuarioService.GetByEmailAsync(email));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create([FromBody] Usuario usuario)
    {
        try
        {
            var newUser = await _usuarioService.CreateAsync(usuario);
            return CreatedAtAction(nameof(GetById), new { id = newUser.id_us }, newUser);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Usuario>> Update(int id, [FromBody] Usuario usuario)
    {
        try
        {
            return Ok(await _usuarioService.UpdateAsync(id, usuario));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _usuarioService.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    
}