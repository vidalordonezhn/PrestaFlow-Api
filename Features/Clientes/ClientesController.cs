using PrestaFlow.API.Features.Clientes.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.Clientes
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todas las operaciones de clientes
    public class ClientesController : ControllerBase
    {
        private readonly ClientesService _clientesService;

        public ClientesController(ClientesService clientesService)
        {
            _clientesService = clientesService;
        }

        /// <summary>
        /// Obtiene el listado completo de clientes registrados con sus respectivos estados de cartera.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClienteResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _clientesService.GetClientesAsync();
            return Ok(clientes);
        }

        /// <summary>
        /// Obtiene el detalle consolidado de un cliente por su ID, incluyendo historial de créditos.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCliente(int id)
        {
            var cliente = await _clientesService.GetClienteByIdAsync(id);
            if (cliente == null)
            {
                return NotFound(new { mensaje = $"No se encontró ningún cliente con el ID '{id}'." });
            }
            return Ok(cliente);
        }

        /// <summary>
        /// Registra un nuevo deudor en el sistema.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ClienteResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var cliente = await _clientesService.CreateClienteAsync(dto);
                return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
