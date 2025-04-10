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
        if (string.IsNullOrEmpty(entity.name) || string.IsNullOrEmpty(entity.description))
        {
            throw new BadRequestException("El campo nombre y la descripcion es obligatorio.");
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
        category.name = entity.name;
        category.description = entity.description;
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
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
    
    

}