using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Services.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly CustomService _customService;

    public CategoryController(ICategoryService categoryService, CustomService customService)
    {
        _categoryService = categoryService;
        _customService = customService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }
    
    [HttpGet("{id:int}")] 
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(category);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] Category category)
    {
        var newCategory = await _categoryService.CreateAsync(category);
        return Ok(newCategory);
    }
    
    [HttpPut("{id:int}")] 
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] Category category)
    {
        var updatedCategory = await _categoryService.UpdateAsync(id, category);
        return Ok(updatedCategory);
    }
    
    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        return Ok(result);
    }
    
    [HttpGet("enterprise/{enterpriseId:int}")] 
    public async Task<IActionResult> GetByEnterpriseIdAsync(int enterpriseId)
    {
        var categories = await _customService.GetCategoriesByEnterprise(enterpriseId);
        return Ok(categories);
    }
    
}