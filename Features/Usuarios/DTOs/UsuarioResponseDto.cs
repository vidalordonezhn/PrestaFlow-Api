using System;

namespace PrestaFlow.API.Features.Usuarios.DTOs
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public bool Activo { get; set; }
        public string FechaCreacion { get; set; } = null!;
    }
}
