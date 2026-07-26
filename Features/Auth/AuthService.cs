using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Auth.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace PrestaFlow.API.Features.Auth
{
    public class AuthService
    {
        private readonly PrestaFlowDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(PrestaFlowDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Valida las credenciales del usuario y genera un token JWT.
        /// </summary>
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            // 1. Buscar el usuario en la base de datos
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Activo);

            // 2. Si no existe o está inactivo, retornar null
            if (usuario is null)
                return null;

            // 3. Verificar la contraseña usando BCrypt
            bool passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
            if (!passwordValida)
                return null;

            // 4. Generar el token JWT
            var token = GenerarToken(usuario);

            return new LoginResponseDto
            {
                Token = token.TokenString,
                ExpiracionMinutos = token.ExpiracionMinutos,
                Username = usuario.Username,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol
            };
        }

        /// <summary>
        /// Genera el token JWT firmado con los claims de identidad del usuario.
        /// </summary>
        private (string TokenString, int ExpiracionMinutos) GenerarToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var ahora = DateTime.UtcNow;
            var expiracion = ahora.AddMinutes(expirationMinutes);

            // Claims: datos integrados en el payload del JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("id", usuario.Id.ToString()),
                new Claim("nombre", usuario.Nombre)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiracion,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expirationMinutes);
        }
    }
}
