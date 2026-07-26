using PrestaFlow.API.Features.Usuarios.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.Usuarios
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Solo administradores pueden gestionar usuarios
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosService _usuariosService;

        public UsuariosController(UsuariosService usuariosService)
        {
            _usuariosService = usuariosService;
        }

        /// <summary>
        /// Obtiene el listado de todos los usuarios del sistema.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<UsuarioResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _usuariosService.GetUsuariosAsync();
            return Ok(usuarios);
        }

        /// <summary>
        /// Registra un nuevo usuario en la base de datos (Admin o Cobrador).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var usuario = await _usuariosService.RegistrarUsuarioAsync(dto);
                return Ok(usuario);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario en el sistema.
        /// </summary>
        [HttpPut("password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _usuariosService.CambiarPasswordAsync(dto.Username, dto.NewPassword);
                return Ok(new { mensaje = "Contraseña actualizada con éxito." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
