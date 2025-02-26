using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class UsuarioService : IUsuarioService
{
    private readonly ApplicationDbContext _context;

    public UsuarioService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)       // Carga la relación con Rol
            .Include(u => u.Empresa)   // Carga la relación con Empresa
            .ToListAsync();
    }

    public async Task<Usuario> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Empresa)
            .FirstOrDefaultAsync(u => u.id_us == id);

        if (usuario == null)
            throw new NotFoundException("Error 404: Usuario no encontrado.");
        return usuario;
    }

    public async Task<Usuario> GetByEmailAsync(string email)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.email_us == email);
        if (usuario == null)
            throw new NotFoundException("Error 404:Usuario con ese correo no encontrado.");
        return usuario;
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        if (string.IsNullOrEmpty(usuario.email_us) || string.IsNullOrEmpty(usuario.password_us))
            throw new BadRequestException("El email y la contraseña son obligatorios.");

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> UpdateAsync(int id, Usuario usuario)
    {
        var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.id_us == id);
        if (usuarioExistente == null)
            throw new NotFoundException("Error 404: Usuario no encontrado.");

        usuarioExistente.name_us = usuario.name_us;
        usuarioExistente.lastname_us = usuario.lastname_us;
        usuarioExistente.email_us = usuario.email_us;
        usuarioExistente.password_us = usuario.password_us;
        usuarioExistente.dni_us = usuario.dni_us;
        usuarioExistente.image_us = usuario.image_us;

        await _context.SaveChangesAsync();
        return usuarioExistente;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            throw new NotFoundException("Error 404: Usuario no encontrado.");

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }
}