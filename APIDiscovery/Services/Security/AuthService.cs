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

        public async Task<string> Authenticate(string email, string password)
        {
            // Buscar usuario
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