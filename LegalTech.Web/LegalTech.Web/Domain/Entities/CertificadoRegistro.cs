using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Título oficial de concesión de marca emitido por DIGEPIH.
/// Incluye la Regla Infranqueable de Solvencia (Párrafo 360 del Manual).
/// </summary>
public class CertificadoRegistro
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpedienteId { get; set; }
    public Expediente Expediente { get; set; } = null!;

    public string NumeroRegistroOficial { get; set; } = string.Empty;
    public DateTime FechaConcesion { get; set; }
    
    // Vigencia legal de diez (10) años en Honduras
    public DateTime FechaVencimientoDecenal { get; set; }

    // Ubicación Física Parametrizable en BD (Tabla UbicacionesArchivo)
    public int? UbicacionArchivoId { get; set; }
    public UbicacionArchivo? UbicacionArchivo { get; set; }

    // =========================================================================
    // REGLA INFRANQUEABLE DE NEGOCIO (P360):
    // "Confirmar solvencia antes de remitir certificados finales al cliente."
    // El sistema bloquea el despacho físico y la descarga digital si es false.
    // =========================================================================
    public bool SolvenciaValidada { get; set; } = false;
    public DateTime? FechaValidacionSolvencia { get; set; }
    public string? ValidadoPorUsuario { get; set; }

    public DateTime? FechaEntregaCliente { get; set; }
    public string? FirmaRecepcionClienteUri { get; set; }
}
