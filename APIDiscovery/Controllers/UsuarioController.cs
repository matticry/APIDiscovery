using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly RabbitMQService _rabbitMqService;

    public UsuarioController(IUsuarioService usuarioService, RabbitMQService rabbitMqService)
    {
        _usuarioService = usuarioService;
        _rabbitMqService = rabbitMqService; 
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Usuario>>> GetAll()
    {
        var usuarios = await _usuarioService.GetAllAsync();
        
        _rabbitMqService.PublishUserAction(new UserActionEvent
        {
            Action = "GetAllUsers",
            CreatedAt = DateTime.Now,
            Username = User.Identity?.Name ?? "admin@admin.com",
            Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
        });
        
        return Ok(usuarios);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Usuario>> GetById(int id)
    {
        try
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"GetUserById: {id}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(usuario);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedGetUserById: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpGet("email/{email}")]
    public async Task<ActionResult<Usuario>> GetByEmail(string email)
    {
        try
        {
            var usuario = await _usuarioService.GetByEmailAsync(email);
            
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"GetUserByEmail: {email}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(usuario);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedGetUserByEmail: {email} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create([FromForm] UsuarioRequest usuarioRequest)
    {
        try
        {
            var newUser = await _usuarioService.CreateAsync(usuarioRequest);
            
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"CreateUser: {newUser.id_us} - {newUser.email_us}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return CreatedAtAction(nameof(GetById), new { id = newUser.id_us }, newUser);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedCreateUser: NotFound - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedCreateUser: BadRequest - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Usuario>> Update(int id, [FromForm] UsuarioRequest usuarioRequest)
    {
        try
        {
            var updatedUser = await _usuarioService.UpdateAsync(id, usuarioRequest);
            
            // Registrar la actualización de usuario
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"UpdateUser: {id} - {updatedUser.email_us}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return Ok(updatedUser);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedUpdateUser: {id} - NotFound - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedUpdateUser: {id} - BadRequest - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _usuarioService.DeleteAsync(id);
            
            // Registrar la eliminación de usuario
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"DeleteUser: {id}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"FailedDeleteUser: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            
            return NotFound(new { message = ex.Message });
        }
    }
}