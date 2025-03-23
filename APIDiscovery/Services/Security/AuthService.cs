using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;

namespace APIDiscovery.Services.Security;

    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config; 
        private readonly EmailService _emailService;

        public AuthService(ApplicationDbContext context, IConfiguration config, EmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }
        public async Task<ForgotPasswordResponseDto> ForgotPassword(string email)
        {
            var startTime = DateTime.Now;
            
            // Verificar si el correo existe
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email_us == email);
            
            if (usuario == null)
            {
                throw new BadRequestException("No existe un usuario con el correo proporcionado.");
            }
            
            // Generar código de 6 dígitos
            var random = new Random();
            string verificationCode = random.Next(100000, 999999).ToString();
            
            // Crear o actualizar token
            var existingToken = await _context.Tokens
                .FirstOrDefaultAsync(t => t.UserId == usuario.id_us);
            
            if (existingToken != null)
            {
                existingToken.TokenString = verificationCode;
                existingToken.ExpiresAt = DateTime.UtcNow.AddMinutes(15);
                existingToken.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var token = new Token
                {
                    UserId = usuario.id_us,
                    TokenString = verificationCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
                
                _context.Tokens.Add(token);
            }
            
            await _context.SaveChangesAsync();
            
            // Enviar correo con el código
            await _emailService.SendVerificationCodeAsync(email, verificationCode);
            
            var endTime = DateTime.Now;
            var responseTimeMs = (endTime - startTime).TotalMilliseconds;
            
            return new ForgotPasswordResponseDto
            {
                Message = "Se ha enviado un código de verificación a tu correo electrónico.",
                ResponseTimeMs = responseTimeMs
            };
        }

        public async Task<ResetPasswordResponseDto> VerifyCodeAndResetPassword(string email, string code, string newPassword)
        {
            var startTime = DateTime.Now;
            
            // Buscar usuario por email
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email_us == email);
            
            if (usuario == null)
            {
                throw new BadRequestException("No existe un usuario con el correo proporcionado.");
            }
            
            // Verificar si hay un token válido
            var token = await _context.Tokens
                .FirstOrDefaultAsync(t => t.UserId == usuario.id_us && 
                                       t.TokenString == code && 
                                       t.ExpiresAt > DateTime.UtcNow);
            
            if (token == null)
            {
                throw new BadRequestException("El código de verificación es inválido o ha expirado.");
            }
            
            // Actualizar contraseña
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            usuario.password_us = hashedPassword;
            
            // Eliminar el token utilizado
            _context.Tokens.Remove(token);
            
            await _context.SaveChangesAsync();
            
            var endTime = DateTime.Now;
            var responseTimeMs = (endTime - startTime).TotalMilliseconds;
            
            return new ResetPasswordResponseDto
            {
                Message = "Tu contraseña ha sido actualizada exitosamente.",
                ResponseTimeMs = responseTimeMs
            };
        }
        


        public async Task<LoginResponseDto> LoginWithRole(string dni, string password, string role)
        {
            var startTime = DateTime.Now;
    
            var validateStatus = await _context.Usuarios.FirstOrDefaultAsync(u => u.status_us == 'I' && u.dni_us == dni);
            if (validateStatus != null)
            {
                throw new BadRequestException("El usuario se encuentra inactivo.");
            }
    
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.name_rol == role);
            if (rol == null)
            {
                throw new NotFoundException("Rol no encontrado.");
            }
    
            // Verificar si el correo está verificado
            var emailVerified = await _context.Usuarios.FirstOrDefaultAsync(u => u.email_verified == 'N' && u.dni_us == dni);
            if (emailVerified != null)
            {
                throw new BadRequestException("El correo no ha sido verificado.");
            }
    
            // Buscar el usuario
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.dni_us == dni);
    
            if (usuario == null)
            {
                throw new BadRequestException("Credenciales inválidas.");
            }
    
            if (usuario.Rol.name_rol != role)
            {
                throw new BadRequestException("El usuario no tiene el rol especificado.");
            }
            
            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, usuario.password_us);
            if (!passwordValid)
            {
                throw new BadRequestException("Credenciales inválidas.");
            }
    
            var endTime = DateTime.Now;
            var responseTimeMs = (endTime - startTime).TotalMilliseconds;
    
            return new LoginResponseDto 
            {
                Message = "Inicio de sesión exitoso",
                ResponseTimeMs = responseTimeMs
            };
        }

        public async Task<string> Authenticate(string email, string password)
        {
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email_us == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password_us))
                return null;

            return await GenerateJwtToken(user);
        }

        private async Task<string> GenerateJwtToken(Usuario user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"]));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.email_us),
                new Claim("user_id", user.id_us.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: expiration,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Guardar token en BD
            var tokenEntry = new Token
            {
                UserId = user.id_us,
                TokenString = tokenString,
                ExpiresAt = expiration
            };
            _context.Tokens.Add(tokenEntry);
            await _context.SaveChangesAsync();

            return tokenString;
        }
    }