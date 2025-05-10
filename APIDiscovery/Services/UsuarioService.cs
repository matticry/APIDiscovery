using System.Text.Json;
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
            .Include(u => u.Rol)
            .ToListAsync();
    }

    public async Task<Usuario> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.id_us == id);

        if (usuario == null)
            throw new NotFoundException("Usuario no encontrado.");

        return usuario;
    }

    public async Task<Usuario> GetByEmailAsync(string email)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.email_us == email);

        if (usuario == null)
            throw new NotFoundException("Usuario con ese correo no encontrado.");

        return usuario;
    }

    public async Task<Usuario> CreateAsync(UsuarioRequest usuarioRequest)
    {
        if (!string.IsNullOrEmpty(usuarioRequest.dni_us) && usuarioRequest.dni_us.Length == 10)
        {
            var validarCedula = usuarioRequest.dni_us.ToCharArray();
            if (!VerificaCedula(validarCedula))
                throw new BadRequestException("El número de cédula proporcionado no es válido.");
        }
        else
        {
            throw new BadRequestException("El número de cédula debe tener 10 dígitos.");
        }

        await ValidateEmailWithHunter(usuarioRequest.email_us);

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.name_rol == usuarioRequest.rol);
        if (rol == null)
            throw new NotFoundException("Rol no encontrado.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(usuarioRequest.password_us);

        var existingUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.email_us == usuarioRequest.email_us);
        if (existingUser != null)
            throw new BadRequestException("Ya existe un usuario con el correo proporcionado.");

        var existingDni = await _context.Usuarios.FirstOrDefaultAsync(u => u.dni_us == usuarioRequest.dni_us);
        if (existingDni != null)
            throw new BadRequestException("Ya existe un usuario con el DNI proporcionado.");

        var existingPhone = await _context.Usuarios.FirstOrDefaultAsync(u => u.phone_us == usuarioRequest.phone_us);
        if (existingPhone != null)
            throw new BadRequestException("Ya existe un usuario con el telefono proporcionado.");


        string? imagePath = null;
        if (usuarioRequest.image_us != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{usuarioRequest.image_us.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

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
            id_rol = rol.id_rol,
            nationality_us = usuarioRequest.nationality_us,
            phone_us = usuarioRequest.phone_us,
            gender_us = usuarioRequest.gender_us,
            age_us = usuarioRequest.CalculateAge(usuarioRequest.birthday_us),
            terms_and_conditions = usuarioRequest.terms_and_conditions,
            birthday_us = usuarioRequest.birthday_us
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
        if (!string.IsNullOrEmpty(usuarioRequest.dni_us) && usuarioRequest.dni_us.Length == 10)
        {
            var validarCedula = usuarioRequest.dni_us.ToCharArray();
            if (!VerificaCedula(validarCedula))
                throw new BadRequestException("El número de cédula proporcionado no es válido.");
        }
        else
        {
            throw new BadRequestException("El número de cédula debe tener 10 dígitos.");
        }

        await ValidateEmailWithHunter(usuarioRequest.email_us);

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.name_rol == usuarioRequest.rol);
        if (rol == null)
            throw new NotFoundException("Rol no encontrado.");

        var existingUser =
            await _context.Usuarios.FirstOrDefaultAsync(u => u.email_us == usuarioRequest.email_us && u.id_us != id);
        if (existingUser != null)
            throw new BadRequestException("Ya existe un usuario con el correo proporcionado.");

        var existingDni =
            await _context.Usuarios.FirstOrDefaultAsync(u => u.dni_us == usuarioRequest.dni_us && u.id_us != id);
        if (existingDni != null)
            throw new BadRequestException("Ya existe un usuario con el DNI proporcionado.");

        usuario.name_us = usuarioRequest.name_us;
        usuario.lastname_us = usuarioRequest.lastname_us;
        usuario.email_us = usuarioRequest.email_us;
        usuario.id_rol = rol.id_rol;
        usuario.dni_us = usuarioRequest.dni_us;

        if (usuarioRequest.image_us != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{usuarioRequest.image_us.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

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
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), usuario.image_us.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }

    private static bool VerificaCedula(char[] validarCedula)
    {
        int aux = 0, par = 0, impar = 0, verifi;
        for (var i = 0; i < 9; i += 2)
        {
            aux = 2 * int.Parse(validarCedula[i].ToString());
            if (aux > 9)
                aux -= 9;
            par += aux;
        }

        for (var i = 1; i < 9; i += 2) impar += int.Parse(validarCedula[i].ToString());

        aux = par + impar;
        if (aux % 10 != 0)
            verifi = 10 - aux % 10;
        else
            verifi = 0;
        if (verifi == int.Parse(validarCedula[9].ToString()))
            return true;
        return false;
    }

    public async Task<Usuario> GetByDniAsync(string dni)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.dni_us == dni);

        if (usuario == null)
            throw new NotFoundException("Usuario con ese DNI no encontrado.");

        return usuario;
    }

    private async Task ValidateEmailWithHunter(string email)
    {
        var apiKey = Environment.GetEnvironmentVariable("HUNTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new BadRequestException("La clave de la API de Hunter no está configurada.");

        using var httpClient = new HttpClient();
        var response =
            await httpClient.GetAsync($"https://api.hunter.io/v2/email-verifier?email={email}&api_key={apiKey}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("status", out var status))
            {
                var emailStatus = status.GetString() ?? string.Empty;

                if (emailStatus != "valid")
                    throw new BadRequestException("El correo electrónico proporcionado no es válido o no existe.");
            }
        }
        else
        {
            throw new BadRequestException("No se pudo verificar el correo electrónico. Intente nuevamente más tarde.");
        }
    }
}