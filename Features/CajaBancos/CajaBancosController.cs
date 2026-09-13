using PrestaFlow.API.Features.CajaBancos.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.CajaBancos
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todas las operaciones de tesorería
    public class CajaBancosController : ControllerBase
    {
        private readonly CajaBancosService _cajaBancosService;

        public CajaBancosController(CajaBancosService cajaBancosService)
        {
            _cajaBancosService = cajaBancosService;
        }

        /// <summary>
        /// Obtiene el listado de cajas generales y cuentas bancarias con sus saldos actualizados.
        /// </summary>
        [HttpGet("cuentas")]
        [ProducesResponseType(typeof(List<CuentaResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCuentas()
        {
            var cuentas = await _cajaBancosService.GetCuentasAsync();
            return Ok(cuentas);
        }

        /// <summary>
        /// Registra una nueva cuenta financiera (Caja o Banco).
        /// </summary>
        [HttpPost("cuenta")]
        [ProducesResponseType(typeof(CuentaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearCuenta([FromBody] CuentaCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cuenta = await _cajaBancosService.CrearCuentaAsync(dto);
            return Ok(cuenta);
        }

        /// <summary>
        /// Obtiene la lista histórica de todos los movimientos de caja y banco.
        /// </summary>
        [HttpGet("transacciones")]
        [ProducesResponseType(typeof(List<TransaccionResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransacciones()
        {
            var transacciones = await _cajaBancosService.GetTransaccionesAsync();
            return Ok(transacciones);
        }

        /// <summary>
        /// Registra un depósito o retiro manual en una cuenta financiera seleccionada.
        /// </summary>
        [HttpPost("transaccion")]
        [ProducesResponseType(typeof(TransaccionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarTransaccion([FromBody] TransaccionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var transaccion = await _cajaBancosService.RegistrarTransaccionAsync(dto);
                return Ok(transaccion);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Realiza el traslado de fondos entre una cuenta de origen y otra de destino.
        /// </summary>
        [HttpPost("transferencia")]
        [ProducesResponseType(typeof(List<TransaccionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarTransferencia([FromBody] TransferenciaCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var transacciones = await _cajaBancosService.RegistrarTransferenciaAsync(dto);
                return Ok(transacciones);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Genera el arqueo y cierre diario de caja y bancos.
        /// </summary>
        [HttpGet("arqueo-diario")]
        [ProducesResponseType(typeof(ArqueoDiarioDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetArqueoDiario([FromQuery] DateTime? fecha)
        {
            var arqueo = await _cajaBancosService.GetArqueoDiarioAsync(fecha);
            return Ok(arqueo);
        }
    }
}
