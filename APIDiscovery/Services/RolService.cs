using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class RolService : IRolService
{
    protected ApplicationDbContext _context;

    public RolService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Rol>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
        
    }

    public async Task<Rol> GetByIdAsync(int id)
    {
        var rol = _context.Roles.FirstOrDefault(r => r.id_rol == id);
        if (rol == null)
        {
            throw new NotFoundException("Rol no encontrado.");
        }
        return rol;
    }

    public async Task<Rol> CreateAsync(Rol entity)
    {
        if (string.IsNullOrEmpty(entity.name_rol) || string.IsNullOrEmpty(entity.name_rol))
        {
            throw new BadRequestException("El campo nombre del rol y el estado es obligatorio.");
        }

        _context.Roles.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Rol> UpdateAsync(int id, Rol entity)
    {
        var rol = _context.Roles.FirstOrDefault(r => r.id_rol == id);
        if (rol == null)
        {
            throw new NotFoundException("Rol no encontrado.");
        }
        rol.name_rol = entity.name_rol;
        await _context.SaveChangesAsync();
        return rol;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.id_rol == id);
        if (rol == null)
        {
            throw new NotFoundException("Rol no encontrado.");
        }
        _context.Roles.Remove(rol);
        await _context.SaveChangesAsync();
        return true;
    }
}