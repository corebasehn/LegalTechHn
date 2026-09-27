using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Feriado o asueto nacional oficial en Honduras (Art. 42 Ley de Procedimiento Administrativo).
/// Los días feriados no computan como días hábiles procesales ante DIGEPIH / IP.
/// </summary>
public class FeriadoNacional
{
    public int Id { get; set; }
    
    /// <summary>
    /// Fecha específica del feriado.
    /// </summary>
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Nombre oficial del feriado o asueto gubernamental.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Base legal o decreto (Ej: "Código de Trabajo Art. 339", "Decreto 78-2015 Semana Morazánica").
    /// </summary>
    public string? FundamentoLegal { get; set; }

    /// <summary>
    /// Indica si el feriado tiene fecha fija anual (Ej: 1 de enero, 15 de septiembre).
    /// Si es falso, es un asueto móvil (Ej: Semana Santa, Semana Morazánica, decretos ejecutivos imprevistos).
    /// </summary>
    public bool EsFijoAnual { get; set; } = true;

    /// <summary>
    /// Indica si está habilitado para el cómputo de plazos.
    /// </summary>
    public bool Activo { get; set; } = true;
}
