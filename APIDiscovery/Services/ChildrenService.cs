using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class ChildrenService : IChildrenService
{
    public ApplicationDbContext _context;
    
    public ChildrenService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Children>> GetAllAsync()
    {
        return await _context.Children.ToListAsync();
        
    }

    public async Task<Children> GetByIdAsync(int id)
    {
        return await _context.Children.FirstOrDefaultAsync(c => c.id_ch == id);
    }

    public async Task<Children> CreateAsync(Children entity)
    {
        var existingChildrenName = await _context.Children.FirstOrDefaultAsync(c => c.name_ch == entity.name_ch);
        if (existingChildrenName != null)
        {
            throw new BadRequestException("El nombre del hijo ya existe.");
        }
        var existingChildrenDni = await _context.Children.FirstOrDefaultAsync(c => c.dni_ch == entity.dni_ch);
        if (existingChildrenDni != null)
        {
            throw new BadRequestException("La cedula del hijo ya existe.");
        }
        
        _context.Children.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
        
    }

    public async  Task<Children> UpdateAsync(int id, Children entity)
    {
        var children = await _context.Children.FirstOrDefaultAsync(c => c.id_ch == id);
        if (children == null)
        {
            throw new NotFoundException("Hijo no encontrado.");
        }
        children.name_ch = entity.name_ch;
        children.dni_ch = entity.dni_ch;
        await _context.SaveChangesAsync();
        return children;
        
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var children = await _context.Children.FirstOrDefaultAsync(c => c.id_ch == id);
        if (children == null)
        {
            throw new NotFoundException("Hijo no encontrado.");
        }
        _context.Children.Remove(children);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<List<Children>> GetChildrenByUserIdAsync(int userId)
    {
        return await _context.Children.Where(c => c.id_usu == userId).ToListAsync();
    }
}