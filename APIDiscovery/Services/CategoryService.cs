using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class CategoryService : ICategoryService
{
    
    private readonly ApplicationDbContext _context;
    
    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories.ToListAsync();
        
    }

    public async Task<Category> GetByIdAsync(int id)
    {
        return (await _context.Categories.FirstOrDefaultAsync(c => c.id_ca == id))!;
    }

    public async Task<Category> CreateAsync(Category entity)
    {
        if (string.IsNullOrEmpty(entity.name))
        {
            throw new BadRequestException("El campo nombre  es obligatorio.");
        }
        
        var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.name == entity.name);
        if (existingCategory != null)
        {
            throw new BadRequestException("Ya existe una categoria con el mismo nombre.");
        }
        
        _context.Categories.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Category> UpdateAsync(int id, Category entity)
    {
        var category = _context.Categories.FirstOrDefault(c => c.id_ca == id);
        if (category == null)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }
        
        var status = await _context.Articles.FirstOrDefaultAsync(a => a.id_category == id);
        if (status != null)
        {
            throw new BadRequestException("No se puede actualizar la categoria porque tiene articulos asociados.");
        }
        category.name = entity.name;
        category.description = entity.description;
        category.status = entity.status;
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.id_ca == id);
        if (category == null)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }
        
        var articles = await _context.Articles.Where(a => a.id_category == id).ToListAsync();
        if (articles.Count > 0)
        {
            throw new BadRequestException("No se puede eliminar la categoria porque tiene articulos asociados.");
        }
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
    
    

}