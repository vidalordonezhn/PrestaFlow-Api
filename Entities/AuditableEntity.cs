using System;

namespace PrestaFlow.API.Entities
{
    public abstract class AuditableEntity
    {
        public DateTime FechaCreacion { get; set; }
        public string CreadoPor { get; set; } = "sistema";
        public DateTime? FechaModificacion { get; set; }
        public string? ModificadoPor { get; set; }
    }
}
