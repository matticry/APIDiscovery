using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmpresaController : ControllerBase
{

    private readonly IEmpresaService _empresaService;
    private readonly RabbitMQService _rabbitMqService;
    
    public EmpresaController(IEmpresaService empresaService, RabbitMQService rabbitMqService)
    {
        _empresaService = empresaService;
        _rabbitMqService = rabbitMqService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empresa>>> GetAll()
    {
        var empresas = await _empresaService.GetAllAsync();
        
        // Registrar la acción de consulta de empresas
        _rabbitMqService.PublishUserAction(new UserActionEvent
        {
            Action = "Consulta de empresas",
            CreatedAt = DateTime.Now,
            Username = User.Identity?.Name ?? "admin@admin.com",
            Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
        });
        
        return Ok(empresas);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Empresa>> GetById(int id)
    {
        try
        {
            var empresa = await _empresaService.GetByIdAsync(id);
            
            // Registrar la acción de consulta por ID
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Consulta de empresa por ID: {id}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(empresa);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Consulta fallida de empresa por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Empresa>> Create([FromBody] Empresa empresaRequest)
    {
        try
        {
            var newEmpresa = await _empresaService.CreateAsync(empresaRequest);
            
            // Registrar la creación de empresa
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Creación de empresa: {newEmpresa.id_empresa} - {newEmpresa.name_empresa}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return CreatedAtAction(nameof(GetById), new { id = newEmpresa.id_empresa }, newEmpresa);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Creación fallida de empresa (NotFound): {ex.Message}",
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
                Action = $"Creación fallida de empresa (BadRequest): {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Empresa>> Update(int id, [FromBody] Empresa empresaRequest)
    {
        try
        {
            var updatedEmpresa = await _empresaService.UpdateAsync(id, empresaRequest);
            
            // Registrar la actualización de empresa
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Actualización de empresa: {id} - {updatedEmpresa.name_empresa}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(updatedEmpresa);
        }
        catch (NotFoundException ex)
        {
            // Registrar intento fallido
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Actualización fallida de empresa (NotFound): {id} - {ex.Message}",
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
                Action = $"Actualización fallida de empresa (BadRequest): {id} - {ex.Message}",
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
            var result = await _empresaService.DeleteAsync(id);
            
            // Registrar la eliminación de empresa
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Eliminación de empresa: {id}",
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
                Action = $"Eliminación fallida de empresa: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }
}