using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Usuarios.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Usuarios
{
    public class UsuariosService
    {
        private readonly PrestaFlowDbContext _context;

        public UsuariosService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los usuarios registrados en el sistema.
        /// </summary>
        public async Task<List<UsuarioResponseDto>> GetUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return usuarios.Select(u => new UsuarioResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Nombre = u.Nombre,
                Rol = u.Rol,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion.ToString("dd/MM/yyyy")
            }).ToList();
        }

        /// <summary>
        /// Registra un nuevo usuario en la base de datos con contraseña encriptada usando BCrypt.
        /// </summary>
        public async Task<UsuarioResponseDto> RegistrarUsuarioAsync(UsuarioCreateDto dto)
        {
            // Validar si el nombre de usuario ya existe
            var existe = await _context.Usuarios.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (existe)
            {
                throw new InvalidOperationException($"El nombre de usuario '{dto.Username}' ya está registrado.");
            }

            // Encriptar contraseña
            string hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var usuario = new Usuario
            {
                Username = dto.Username,
                PasswordHash = hash,
                Nombre = dto.Nombre,
                Rol = dto.Rol,
                Activo = true
            };

            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            return new UsuarioResponseDto
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion.ToString("dd/MM/yyyy")
            };
        }

        /// <summary>
        /// Cambia la contraseña de un usuario en el sistema.
        /// </summary>
        public async Task CambiarPasswordAsync(string username, string newPassword)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (usuario == null)
            {
                throw new InvalidOperationException($"El usuario '{username}' no existe.");
            }

            // Encriptar nueva contraseña usando BCrypt
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
        }
    }
}
