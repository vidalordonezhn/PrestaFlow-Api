using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestaFlow.API.Features.Reportes.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Reportes
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ReportesService _reportesService;

        public ReportesController(ReportesService reportesService)
        {
            _reportesService = reportesService;
        }

        [HttpGet("cartera")]
        public async Task<ActionResult<ResumenCarteraDto>> GetResumenCartera()
        {
            var result = await _reportesService.GetResumenCarteraAsync();
            return Ok(result);
        }

        [HttpGet("ingresos")]
        public async Task<ActionResult<IngresosReporteDto>> GetIngresosReporte(
            [FromQuery] string? startDate = null, 
            [FromQuery] string? endDate = null)
        {
            DateTime start = DateTime.UtcNow.AddMonths(-1);
            DateTime end = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
            {
                start = DateTime.SpecifyKind(parsedStart, DateTimeKind.Utc);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
            {
                end = DateTime.SpecifyKind(parsedEnd.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc); // Ir hasta final de día
            }

            var result = await _reportesService.GetIngresosReporteAsync(start, end);
            return Ok(result);
        }

        [HttpGet("mora")]
        public async Task<ActionResult<List<MoraDeudorDto>>> GetDeudoresMora()
        {
            var result = await _reportesService.GetDeudoresMoraAsync();
            return Ok(result);
        }
    }
}
