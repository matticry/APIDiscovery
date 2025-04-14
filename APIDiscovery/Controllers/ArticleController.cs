using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs;
using APIDiscovery.Services.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIDiscovery.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly CustomService _categoryService;
    private readonly IFareService _fareService;
    
    public ArticleController(
        IArticleService articleService,
        CustomService categoryService,
        IFareService fareService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _fareService = fareService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateArticle([FromBody] ArticleCreateDto articleDto)
    {
        var response = await _articleService.CreateArticle(articleDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetArticleById(int id)
    {
        var response = await _articleService.GetArticleById(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
    
    [HttpGet("enterprise/{enterpriseId}")]
    public async Task<IActionResult> GetArticlesByEnterprise(int enterpriseId)
    {
        var response = await _articleService.GetArticlesByEnterprise(enterpriseId);
        return Ok(response);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] ArticleCreateDto articleDto)
    {
        var response = await _articleService.UpdateArticle(id, articleDto);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var response = await _articleService.DeleteArticle(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }
    
    [HttpGet("categories/{enterpriseId}")]
    public async Task<IActionResult> GetCategoriesByEnterprise(int enterpriseId)
    {
        var response = await _categoryService.GetCategoriesByEnterprise(enterpriseId);
        return Ok(response);
    }
    
    [HttpGet("fares")]
    public async Task<IActionResult> GetAllFares()
    {
        var response = await _fareService.GetAllFares();
        return Ok(response);
    }
}