using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Control del ciclo de las 3 publicaciones en La Gaceta (ENAG) y entrega física a DIGEPIH.
/// </summary>
public class PublicacionENAG
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpedienteId { get; set; }
    public Expediente Expediente { get; set; } = null!;

    public string? NumeroAvisoDIGEPIH { get; set; }
    public string? ReciboPagoENAG { get; set; }

    // Las 3 publicaciones obligatorias (una cada 15 días, ~45 días)
    public DateTime? FechaPublicacionAviso1 { get; set; }
    public DateTime? FechaPublicacionAviso2 { get; set; }
    public DateTime? FechaPublicacionAviso3 { get; set; }

    // Término fatal de oposición: 30 días hábiles posteriores al 3er aviso
    public DateTime? FechaFinPeriodoOposicion { get; set; }

    // Regla de abandono: Adquisición física de los 3 diarios y presentación ante DIGEPIH
    public bool EjemplaresFisicosRecibidos { get; set; }
    public bool EjemplaresEntregadosDIGEPIH { get; set; }
    public DateTime? FechaEntregaEjemplaresDIGEPIH { get; set; }
}
