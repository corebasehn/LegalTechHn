using System;
using LegalTech.Web.Domain.Enums;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Módulo de Litigios: Denegatorias, Oposiciones, Cancelación por No Uso (3 años) y Nulidad.
/// </summary>
public class Litigio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Puede pertenecer a un expediente propio o ser una acción independiente del bufete
    public Guid? ExpedienteId { get; set; }
    public Expediente? Expediente { get; set; }

    public TipoLitigio TipoLitigio { get; set; }

    public string ParteDemandante { get; set; } = string.Empty;
    public string ParteDemandada { get; set; } = string.Empty;
    public string MarcaEnConflicto { get; set; } = string.Empty;

    public DateTime FechaNotificacion { get; set; }
    
    // Motor de plazos: Vencimiento perentorio (60d fondo, 10d contestación oposición, etc.)
    public DateTime FechaVencimientoFatal { get; set; }

    // Autorización del cliente para actuar y asumir costos
    public bool AutorizadoPorCliente { get; set; }

    public string? AbogadoLitigante { get; set; }

    public EstadoLitigio Estado { get; set; } = EstadoLitigio.BorradorEstrategia;

    public string? ObservacionesEstrategia { get; set; }
    public string? ResultadoFinal { get; set; }

    // Auditoría específica para Cancelación por No Uso
    public bool ConstanciaRehabilitacionObtenida { get; set; }
    public bool AuditoriaRedesSocialesRealizada { get; set; } // Instagram, TikTok, Facebook, Google
    public string? ReporteEvidenciaUsoUri { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
