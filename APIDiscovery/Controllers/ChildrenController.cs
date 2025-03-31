using APIDiscovery.Exceptions;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ChildrenController : Controller
{
    private readonly ChildrenService _childrenService;
    private readonly RabbitMQService _rabbitMqService;

    public ChildrenController(ChildrenService childrenService, RabbitMQService rabbitMqService)
    {
        _childrenService = childrenService;
        _rabbitMqService = rabbitMqService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Children>>> GetAllAsync()
    {
        try
        {
            var children = await _childrenService.GetAllAsync();
            return Ok(children);
        }
        catch (Exception ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de obtener hijos - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Children>> GetByIdAsync(int id)
    {
        try
        {
            var children = await _childrenService.GetByIdAsync(id);
            return Ok(children);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de obtener hijo por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Children>> CreateAsync([FromBody] Children childrenRequest)
    {
        try
        {
            var newChildren = await _childrenService.CreateAsync(childrenRequest);
            return Ok(newChildren);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de crear hijo - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"            
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Children>> UpdateAsync(int id, [FromBody] Children childrenRequest)
    {
        try
        {
            var updatedChildren = await _childrenService.UpdateAsync(id, childrenRequest);
            return Ok(updatedChildren);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de actualizar hijo por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteAsync(int id)
    {
        try
        {
            var deleted = await _childrenService.DeleteAsync(id);
            return Ok(deleted);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de eliminar hijo por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpGet("{id}/userChildren")]
    public async Task<ActionResult<Children>> GetByIdUsuario(int id)
    {
        try
        {
            var children = await _childrenService.GetChildrenByUserIdAsync(id);
            return Ok(children);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de obtener hijo por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    
}