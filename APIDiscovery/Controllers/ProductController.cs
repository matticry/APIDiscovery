using APIDiscovery.Exceptions;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : Controller
{
    
    private readonly ProductService _productService;
    private readonly RabbitMQService _rabbitMqService;
    
    public ProductController(ProductService productService, RabbitMQService rabbitMqService)
    {
        _productService = productService;
        _rabbitMqService = rabbitMqService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAllAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync();
            
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Productos obtenidos",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return Ok(products);
        }
        catch (Exception ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de obtener productos - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetByIdAsync(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de obtener producto por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> CreateAsync([FromBody] Product productRequest)
    {
        try
        {
            var newProduct = await _productService.CreateAsync(productRequest);
            return Ok(newProduct);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de crear producto - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateAsync(int id, [FromBody] Product productRequest)
    {
        try
        {
            var updatedProduct = await _productService.UpdateAsync(id, productRequest);
            return Ok(updatedProduct);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de actualizar producto por ID: {id} - {ex.Message}",
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
            var deleted = await _productService.DeleteAsync(id);
            return Ok(deleted);
        }
        catch (NotFoundException ex)
        {
            _rabbitMqService.PublishUserAction(new UserActionEvent
            {
                Action = $"Intento fallido de eliminar producto por ID: {id} - {ex.Message}",
                CreatedAt = DateTime.Now,
                Username = User.Identity?.Name ?? "admin@admin.com",
                Dni = User.Claims.FirstOrDefault(c => c.Type == "dni")?.Value ?? "1755386099"
            });
            return NotFound(new { message = ex.Message });
        }
    }
    
}