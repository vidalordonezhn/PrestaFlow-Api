using PrestaFlow.API.Features.Pagos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.Pagos
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para registrar y ver abonos
    public class PagosController : ControllerBase
    {
        private readonly PagosService _pagosService;

        public PagosController(PagosService pagosService)
        {
            _pagosService = pagosService;
        }

        /// <summary>
        /// Obtiene el listado histórico de abonos/pagos realizados por deudores.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PagoResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPagos()
        {
            var pagos = await _pagosService.GetPagosAsync();
            return Ok(pagos);
        }

        /// <summary>
        /// Registra un pago de cuota, incrementando la tesorería de la cuenta correspondiente.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PagoResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearPago([FromBody] PagoCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var pago = await _pagosService.CrearPagoAsync(dto);
                return CreatedAtAction(nameof(GetPagos), new { id = pago.Id }, pago);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
