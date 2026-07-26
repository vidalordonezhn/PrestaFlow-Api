namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class CuentaResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!; // Caja, Banco
        public decimal Saldo { get; set; }
    }
}
