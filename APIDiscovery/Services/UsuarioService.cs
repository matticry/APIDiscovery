using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
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
                .Include(u => u.Empresa)
                .Include(u => u.Rol)
                .ToListAsync();
        }

        public async Task<Usuario> GetByIdAsync(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.id_us == id);
                
            if (usuario == null)
                throw new NotFoundException("Usuario no encontrado.");
                
            return usuario;
        }

        public async Task<Usuario> GetByEmailAsync(string email)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.email_us == email);

            if (usuario == null)
                throw new NotFoundException("Usuario con ese correo no encontrado.");

            return usuario;
        }

        public async Task<Usuario> CreateAsync(UsuarioRequest usuarioRequest)
        {
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.name_empresa == usuarioRequest.empresa);
            if (empresa == null)
                throw new NotFoundException("Empresa no encontrada.");

            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.name_rol == usuarioRequest.rol);
            if (rol == null)
                throw new NotFoundException("Rol no encontrado.");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(usuarioRequest.password_us);
            
            var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.email_us == usuarioRequest.email_us);
            if (existingUser != null)
                throw new BadRequestException("Ya existe un usuario con el correo proporcionado.");
            
            var existingDni = await _context.Usuarios.FirstOrDefaultAsync(u => u.dni_us == usuarioRequest.dni_us);
            if (existingDni != null)
                throw new BadRequestException("Ya existe un usuario con el DNI proporcionado.");
            
            string? imagePath = null;
            if (usuarioRequest.image_us != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = $"{Guid.NewGuid()}_{usuarioRequest.image_us.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await usuarioRequest.image_us.CopyToAsync(fileStream);
                }

                imagePath = $"/uploads/{uniqueFileName}"; 
            }

            var usuario = new Usuario
            {
                name_us = usuarioRequest.name_us,
                lastname_us = usuarioRequest.lastname_us,
                email_us = usuarioRequest.email_us,
                password_us = hashedPassword, 
                dni_us = usuarioRequest.dni_us, 
                image_us = imagePath,
                id_empresa = empresa.id_empresa, 
                id_rol = rol.id_rol 
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

    public async Task<Usuario> UpdateAsync(int id, UsuarioRequest usuarioRequest)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            throw new NotFoundException("Usuario no encontrado.");

        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.name_empresa == usuarioRequest.empresa);
        if (empresa == null)
            throw new NotFoundException("Empresa no encontrada.");

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.name_rol == usuarioRequest.rol);
        if (rol == null)
            throw new NotFoundException("Rol no encontrado.");

        var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.email_us == usuarioRequest.email_us && u.id_us != id);
        if (existingUser != null)
            throw new BadRequestException("Ya existe un usuario con el correo proporcionado.");

        var existingDni = await _context.Usuarios.FirstOrDefaultAsync(u => u.dni_us == usuarioRequest.dni_us && u.id_us != id);
        if (existingDni != null)
            throw new BadRequestException("Ya existe un usuario con el DNI proporcionado.");

        usuario.name_us = usuarioRequest.name_us;
        usuario.lastname_us = usuarioRequest.lastname_us;
        usuario.email_us = usuarioRequest.email_us;
        usuario.id_empresa = empresa.id_empresa;
        usuario.id_rol = rol.id_rol;
        usuario.dni_us = usuarioRequest.dni_us;

        if (usuarioRequest.image_us != null)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = $"{Guid.NewGuid()}_{usuarioRequest.image_us.FileName}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await usuarioRequest.image_us.CopyToAsync(fileStream);
            }

            usuario.image_us = $"/uploads/{uniqueFileName}";
        }

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            throw new NotFoundException("Usuario no encontrado.");

        if (!string.IsNullOrEmpty(usuario.image_us))
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), usuario.image_us.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }

}
    