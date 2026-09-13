using PrestaFlow.API.Features.Garantias.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Garantias
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GarantiasController : ControllerBase
    {
        private readonly GarantiasService _garantiasService;

        public GarantiasController(GarantiasService garantiasService)
        {
            _garantiasService = garantiasService;
        }

        [HttpGet]
        public async Task<ActionResult<List<GarantiaResponseDto>>> GetGarantias(
            [FromQuery] string? query,
            [FromQuery] string? estado,
            [FromQuery] string? tipo)
        {
            var result = await _garantiasService.GetGarantiasAsync(query, estado, tipo);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GarantiaResponseDto>> GetGarantiaById(int id)
        {
            var result = await _garantiasService.GetGarantiaByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { mensaje = $"No se encontró la garantía con ID #{id}." });
            }
            return Ok(result);
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<List<GarantiaResponseDto>>> GetGarantiasByCliente(int clienteId)
        {
            var result = await _garantiasService.GetGarantiasByClienteAsync(clienteId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<GarantiaResponseDto>> CrearGarantia([FromBody] CrearGarantiaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _garantiasService.CrearGarantiaAsync(dto);
                return CreatedAtAction(nameof(GetGarantiaById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error interno al registrar la garantía.", detalle = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GarantiaResponseDto>> ActualizarGarantia(int id, [FromBody] ActualizarGarantiaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _garantiasService.ActualizarGarantiaAsync(id, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error al actualizar la garantía.", detalle = ex.Message });
            }
        }

        [HttpPatch("{id}/estado")]
        public async Task<ActionResult<GarantiaResponseDto>> CambiarEstado(int id, [FromBody] CambiarEstadoGarantiaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _garantiasService.CambiarEstadoAsync(id, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error al actualizar el estado de custodia.", detalle = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> EliminarGarantia(int id)
        {
            var eliminada = await _garantiasService.EliminarGarantiaAsync(id);
            if (!eliminada)
            {
                return NotFound(new { mensaje = $"No se encontró la garantía con ID #{id} para eliminar." });
            }
            return NoContent();
        }
    }
}
