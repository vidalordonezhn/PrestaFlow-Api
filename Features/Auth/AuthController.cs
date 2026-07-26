using PrestaFlow.API.Features.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Autentica un usuario y devuelve un token JWT.
        /// </summary>
        /// <remarks>
        /// Ejemplo de petición:
        ///
        ///     POST /api/auth/login
        ///     {
        ///        "username": "admin",
        ///        "password": "Admin123*"
        ///     }
        ///
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _authService.LoginAsync(request);

            if (resultado is null)
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

            return Ok(resultado);
        }
    }
}
