using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APIDiscovery.Core;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace APIDiscovery.Services.Security;

    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<(string? token, string? errorMessage)> Authenticate(string email, string password)
        {
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email_us == email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password_us))
                return (null, "Credenciales inválidas");
            
            if (IsDateExpired(user.fecha_arriendo_us))
            {
                return (null, "Su periodo de arriendo ha expirado. Por favor, renueve su suscripción o contacte con soporte.");
            }
        
            var token = await GenerateJwtToken(user);
            return (token, null);
        }

        private async Task<string> GenerateJwtToken(Usuario user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? throw new InvalidOperationException()));

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

        private static bool IsDateExpired(DateTime fechaArriendo)
        {
            var fechaActual = DateTime.Now;
            return fechaActual > fechaArriendo;
        }
    }