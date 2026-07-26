namespace PrestaFlow.API.Features.Auth.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public int ExpiracionMinutos { get; set; }
        public string Username { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Rol { get; set; } = null!;
    }
}
