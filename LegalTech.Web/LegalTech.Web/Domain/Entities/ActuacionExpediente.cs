using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Bitácora histórica e Hitos Procesales de la Línea de Tiempo de un Expediente Marcario.
/// </summary>
public class ActuacionExpediente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpedienteId { get; set; }
    public Expediente Expediente { get; set; } = null!;

    // Relación con el tipo de hito (tabla parametrizable)
    public int TipoActuacionId { get; set; }
    public TipoActuacion TipoActuacion { get; set; } = null!;

    public DateTime FechaActuacion { get; set; } = DateTime.UtcNow;

    public string Titulo { get; set; } = string.Empty;

    public string? Comentario { get; set; }

    public string UsuarioResponsable { get; set; } = "Sistema";

    public string? ArchivoAdjuntoUri { get; set; } // Copia escaneada de providencia, auto o escrito

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
