using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Representa un término legal fatal (días hábiles procesales de Honduras).
/// </summary>
public class PlazoLegal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpedienteId { get; set; }
    public Expediente Expediente { get; set; } = null!;

    public string Concepto { get; set; } = string.Empty; // Ej: Prevención Forma (30d), Objeción Fondo (60d)
    public int DiasHabiles { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimientoFatal { get; set; }

    public bool Cumplido { get; set; }
    public DateTime? FechaCumplimiento { get; set; }
}
