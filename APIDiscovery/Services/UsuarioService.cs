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

            var usuario = new Usuario
            {
                name_us = usuarioRequest.name_us,
                lastname_us = usuarioRequest.lastname_us,
                email_us = usuarioRequest.email_us,
                password_us = hashedPassword, 
                dni_us = usuarioRequest.dni_us,
                image_us = usuarioRequest.image_us,
                id_empresa = empresa.id_empresa, 
                id_rol = rol.id_rol 
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

        public async Task<Usuario> UpdateAsync(int id, Usuario usuario)
        {
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
                throw new NotFoundException("Usuario no encontrado.");

            usuarioExistente.name_us = usuario.name_us;
            usuarioExistente.lastname_us = usuario.lastname_us;
            usuarioExistente.email_us = usuario.email_us;

            if (!string.IsNullOrEmpty(usuario.password_us))
            {
                usuarioExistente.password_us = BCrypt.Net.BCrypt.HashPassword(usuario.password_us);
            }

            usuarioExistente.dni_us = usuario.dni_us;
            usuarioExistente.image_us = usuario.image_us;

            await _context.SaveChangesAsync();
            return usuarioExistente;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                throw new NotFoundException("Usuario no encontrado.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return true;
        }
}
    