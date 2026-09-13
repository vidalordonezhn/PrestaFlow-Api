using PrestaFlow.API.Features.Prestamos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.Prestamos
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todas las operaciones de préstamos
    public class PrestamosController : ControllerBase
    {
        private readonly PrestamosService _prestamosService;

        public PrestamosController(PrestamosService prestamosService)
        {
            _prestamosService = prestamosService;
        }

        /// <summary>
        /// Obtiene el listado de préstamos otorgados.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PrestamoResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPrestamos()
        {
            var prestamos = await _prestamosService.GetPrestamosAsync();
            return Ok(prestamos);
        }

        /// <summary>
        /// Obtiene un préstamo por su identificador único.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PrestamoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrestamoById(int id)
        {
            var prestamo = await _prestamosService.GetPrestamoByIdAsync(id);
            if (prestamo == null)
            {
                return NotFound(new { mensaje = $"No se encontró el préstamo con ID {id}." });
            }
            return Ok(prestamo);
        }

        /// <summary>
        /// Registra un nuevo préstamo y realiza el débito financiero desde la cuenta seleccionada.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PrestamoResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearPrestamo([FromBody] PrestamoCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var prestamo = await _prestamosService.CrearPrestamoAsync(dto);
                // Retornar 201 Created
                return CreatedAtAction(nameof(GetPrestamos), new { id = prestamo.Id }, prestamo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Capitaliza los intereses caídos de una cuota en mora del préstamo, sumándolos al capital principal.
        /// </summary>
        [HttpPost("{id}/capitalizar-interes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CapitalizarInteres(int id, [FromBody] CapitalizarInteresDto dto)
        {
            try
            {
                await _prestamosService.CapitalizarInteresAsync(id, dto.CuotaId);
                return Ok(new { mensaje = "Interés capitalizado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
