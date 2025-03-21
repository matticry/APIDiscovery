using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using APIDiscovery.Services.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RolController : ControllerBase
{
    private readonly IRolService _rolService;
    private readonly RabbitMQService _rabbitMqService;
    private readonly CustomService _customService;
    
    public RolController(IRolService rolService, RabbitMQService rabbitMqService, CustomService customService)
    {
        _rolService = rolService;
        _rabbitMqService = rabbitMqService;
        _customService = customService;
    }
    
    [HttpGet("enfermero-familiar")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnfermeroFamiliarRoles()
    {
        var roles = await _customService.GetResultRoleByNameAsync();
        return Ok(roles);
    }
    
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rol>>> GetAll()
    {
        var roles = await _rolService.GetAllAsync();
        
        // Registrar la acción de consulta de roles
        _rabbitMqService.PublishUserAction(new UserActionEvent
        {
            Action = "Consulta de roles",
            CreatedAt = DateTime.Now,
            Username = User.Identity?.Name ?? "admin@admin.com",
            Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
        });
        
        return Ok(roles);
    }
    
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Rol>> GetById(int id)
    {
        try
        {
            var rol = await _rolService.GetByIdAsync(id);
            
            // Registrar la acción de consulta de rol por ID
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Consulta de rol por ID: {id}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(rol);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Consulta fallida de rol por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Rol>> Create([FromBody] Rol rolRequest)
    {
        try
        {
            var newRol = await _rolService.CreateAsync(rolRequest);
            
            // Registrar la creación de rol
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Creación de rol: {newRol.id_rol} - {newRol.name_rol}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return CreatedAtAction(nameof(GetById), new { id = newRol.id_rol }, newRol);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Creación fallida de rol (NotFound): {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Creación fallida de rol (BadRequest): {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Rol>> Update(int id, [FromBody] Rol rolRequest)
    {
        try
        {
            var updatedRol = await _rolService.UpdateAsync(id, rolRequest);
            
            // Registrar la actualización de rol
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Actualización de rol: {id} - {updatedRol.name_rol}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(updatedRol);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Actualización fallida de rol (NotFound): {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Actualización fallida de rol (BadRequest): {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(int id)
    {
        try
        {
            var result = await _rolService.DeleteAsync(id);
            
            // Registrar la eliminación de rol
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Eliminación de rol: {id}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Eliminación fallida de rol: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }
}