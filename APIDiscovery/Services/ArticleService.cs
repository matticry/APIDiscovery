
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Services;

public class ArticleService : IArticleService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;
    
    public ArticleService(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }
    
    public async Task<ResponseDto> CreateArticle(ArticleCreateDto articleDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            // Verificar que la empresa existe
            var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == articleDto.IdEnterprise);
            if (enterprise == null)
            {
                response.Success = false;
                response.DisplayMessage = "La empresa especificada no existe.";
                return response;
            }
            
            // Verificar que la categoría existe y pertenece a la empresa
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.id_ca == articleDto.IdCategory && c.id_enterprise == articleDto.IdEnterprise);
                
            if (category == null)
            {
                response.Success = false;
                response.DisplayMessage = "La categoría especificada no existe o no pertenece a esta empresa.";
                return response;
            }
            
            // Verificar que todas las tarifas existen
            foreach (var fareId in articleDto.FareIds)
            {
                var fare = await _context.Fares.FirstOrDefaultAsync(f => f.id_fare == fareId);
                if (fare == null)
                {
                    response.Success = false;
                    response.DisplayMessage = $"La tarifa con ID {fareId} no existe.";
                    return response;
                }
            }
            
            // Verificar que el código no esté repetido en la misma empresa
            var existingArticle = await _context.Articles
                .FirstOrDefaultAsync(a => a.code == articleDto.Code && a.id_enterprise == articleDto.IdEnterprise);
                
            if (existingArticle != null)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe un artículo con este código en la empresa.";
                return response;
            }
            
            var imagePath = await _imageService.SaveImageAsync(articleDto.Image);
            
            // Crear el artículo
            var article = new Article
            {
                name = articleDto.Name,
                code = articleDto.Code,
                price_unit = articleDto.PriceUnit,
                stock = articleDto.Stock,
                status = 'A',
                created_at = DateTime.Now,
                update_at = DateTime.Now,
                image = imagePath,
                description = articleDto.Description,
                id_enterprise = articleDto.IdEnterprise,
                id_category = articleDto.IdCategory
            };
            
            await _context.Articles.AddAsync(article);
            await _context.SaveChangesAsync();
            
            // Asociar el artículo con las tarifas seleccionadas
            foreach (var fareId in articleDto.FareIds)
            {
                var tariffArticle = new TariffArticle
                {
                    id_article = article.id_ar,
                    id_fare = fareId
                };
                
                await _context.TariffArticles.AddAsync(tariffArticle);
            }
            
            await _context.SaveChangesAsync();
            
            // Retornar el artículo creado con sus tarifas
            var articleWithFares = await GetArticleDetailById(article.id_ar);
            
            response.Result = articleWithFares;
            response.DisplayMessage = "Artículo creado exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al crear el artículo.";
            response.ErrorMessages = [ex.Message];
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
    
    public async Task<ResponseDto> GetArticleById(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var articleDto = await GetArticleDetailById(id);

            response.Result = articleDto;
            response.DisplayMessage = "Artículo obtenido exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener el artículo.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
    
    public async Task<ResponseDto> GetArticlesByEnterprise(int enterpriseId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var articles = await _context.Articles
                .Where(a => a.id_enterprise == enterpriseId && a.status == 'A')
                .Include(a => a.Category)
                .Include(a => a.TariffArticles)
                    .ThenInclude(ta => ta.Fare)
                        .ThenInclude(f => f.Tax)
                .ToListAsync();
                
            var articlesDto = articles.Select(a => new ArticleDto
            {
                Id = a.id_ar,
                Name = a.name,
                Code = a.code,
                PriceUnit = a.price_unit,
                Stock = a.stock,
                Status = a.status.ToString(),
                CreatedAt = a.created_at,
                UpdateAt = a.update_at,
                Image = a.image,
                Description = a.description,
                IdEnterprise = a.id_enterprise,
                IdCategory = a.id_category,
                CategoryName = a.Category.name,
                Fares = a.TariffArticles.Select(ta => new FareDto
                {
                    Id = ta.Fare.id_fare,
                    Percentage = ta.Fare.percentage,
                    Description = ta.Fare.description,
                    IdTax = ta.Fare.id_tax,
                    TaxDescription = ta.Fare.Tax.description
                }).ToList()
            }).ToList();
            
            response.Result = articlesDto;
            response.DisplayMessage = "Artículos obtenidos exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al obtener los artículos.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
    
    public async Task<ResponseDto> UpdateArticle(int id, ArticleCreateDto articleDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.id_ar == id);
            
            if (article == null)
            {
                response.Success = false;
                response.DisplayMessage = "Artículo no encontrado.";
                return response;
            }
            
            // Verificar que el código no esté repetido en la misma empresa (excepto para el mismo artículo)
            var existingArticle = await _context.Articles
                .FirstOrDefaultAsync(a => a.code == articleDto.Code && a.id_enterprise == articleDto.IdEnterprise && a.id_ar != id);
                
            if (existingArticle != null)
            {
                response.Success = false;
                response.DisplayMessage = "Ya existe otro artículo con este código en la empresa.";
                return response;
            }
            
            // Verificar que la categoría existe y pertenece a la empresa
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.id_ca == articleDto.IdCategory && c.id_enterprise == articleDto.IdEnterprise);
                
            if (category == null)
            {
                response.Success = false;
                response.DisplayMessage = "La categoría especificada no existe o no pertenece a esta empresa.";
                return response;
            }
            
            var imagePath = article.image;
            if (articleDto.Image != null)
            {
                imagePath = await _imageService.UpdateImageAsync(article.image, articleDto.Image);
            }
            
            // Actualizar los datos del artículo
            article.name = articleDto.Name;
            article.code = articleDto.Code;
            article.price_unit = articleDto.PriceUnit;
            article.stock = articleDto.Stock;
            article.update_at = DateTime.Now;
            article.image = imagePath;
            article.description = articleDto.Description;
            article.id_category = articleDto.IdCategory;
            
            // Eliminar todas las asociaciones existentes de tarifa
            var existingTariffs = await _context.TariffArticles
                .Where(ta => ta.id_article == id)
                .ToListAsync();
                
            _context.TariffArticles.RemoveRange(existingTariffs);
            
            // Crear las nuevas asociaciones de tarifa
            foreach (var fareId in articleDto.FareIds)
            {
                var tariffArticle = new TariffArticle
                {
                    id_article = id,
                    id_fare = fareId
                };
                
                await _context.TariffArticles.AddAsync(tariffArticle);
            }
            
            await _context.SaveChangesAsync();
            
            // Retornar el artículo actualizado con sus tarifas
            var articleWithFares = await GetArticleDetailById(id);
            
            response.Result = articleWithFares;
            response.DisplayMessage = "Artículo actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al actualizar el artículo.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
    
    public async Task<ResponseDto> DeleteArticle(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ResponseDto();
        
        try
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.id_ar == id);
            
            if (article == null)
            {
                response.Success = false;
                response.DisplayMessage = "Artículo no encontrado.";
                return response;
            }
            
            if (!string.IsNullOrEmpty(article.image))
            {
                _imageService.DeleteImage(article.image);
            }
            
            // Marcaar como inactivo en lugar de eliminar físicamente
            article.status = 'I';
            article.update_at = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            response.DisplayMessage = "Artículo eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.DisplayMessage = "Error al eliminar el artículo.";
            response.ErrorMessages = new List<string> { ex.Message };
        }
        
        stopwatch.Stop();
        response.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        
        return response;
    }
    
    private async Task<ArticleDto> GetArticleDetailById(int id)
    {
        var article = await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.TariffArticles)
                .ThenInclude(ta => ta.Fare)
                    .ThenInclude(f => f.Tax)
            .FirstOrDefaultAsync(a => a.id_ar == id);
            
        if (article == null)
        {
            return null;
        }
        
        return new ArticleDto
        {
            Id = article.id_ar,
            Name = article.name,
            Code = article.code,
            PriceUnit = article.price_unit,
            Stock = article.stock,
            Status = article.status.ToString(),
            CreatedAt = article.created_at,
            UpdateAt = article.update_at,
            Image = article.image,
            Description = article.description,
            IdEnterprise = article.id_enterprise,
            IdCategory = article.id_category,
            CategoryName = article.Category.name,
            Fares = article.TariffArticles.Select(ta => new FareDto
            {
                Id = ta.Fare.id_fare,
                Percentage = ta.Fare.percentage,
                Description = ta.Fare.description,
                IdTax = ta.Fare.id_tax,
                TaxDescription = ta.Fare.Tax.description
            }).ToList()
        };
    }
}

