namespace LegalTech.Web.Domain.Enums;

/// <summary>
/// Estados de cobranza, aprobación y facturación de expedientes marcarios.
/// </summary>
public enum EstadoFactura
{
    Borrador = 1,
    Emitida = 2,
    Pagada = 3,
    Parcial = 4,
    Anulada = 5,
    PendienteAprobacion = 6,
    Rechazada = 7
}
