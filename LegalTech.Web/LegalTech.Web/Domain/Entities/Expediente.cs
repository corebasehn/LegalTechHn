using System;
using System.Collections.Generic;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Expediente principal del trámite marcario ante la DIGEPIH (Honduras).
/// </summary>
public class Expediente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Control interno del bufete (Ej: EXP-2026-0001)
    public string CodigoInterno { get; set; } = string.Empty;

    // Relación con el Titular
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Datos del Signo Distintivo
    public string DenominacionMarca { get; set; } = string.Empty;

    // Relación parametrizable con TipoSigno (Tabla en Base de Datos)
    public int TipoSignoId { get; set; }
    public TipoSigno TipoSigno { get; set; } = null!;
    
    // Clasificación Internacional de Niza (1 a 45)
    public int ClaseNiza { get; set; }

    // Criterio de la Oficina de Marcas de Honduras: prohibido generalidades, debe ser específica
    public string DescripcionEspecificaProductosServicios { get; set; } = string.Empty;

    // Reivindicación de Prioridad (Convenio de París)
    public bool ReivindicaPrioridad { get; set; }
    public string? PaisPrioridad { get; set; }
    public DateTime? FechaPrioridad { get; set; }
    public string? NumeroSolicitudPrioridad { get; set; }

    // Datos de Presentación Oficial DIGEPIH
    public string? NumeroExpedienteDIGEPIH { get; set; }
    public DateTime? FechaPresentacionOficial { get; set; } // Establece prioridad legal en Honduras
    public string? ComprobanteRecepcionUri { get; set; }

    // Vía de Presentación Oficial (Tabla parametrizable)
    public int? ViaPresentacionId { get; set; }
    public ViaPresentacion? ViaPresentacion { get; set; }

    // Estado Procesal Actual (Tabla parametrizable en BD)
    public int EstadoProcesalId { get; set; } = 1;
    public EstadoProcesal EstadoProcesal { get; set; } = null!;

    // Requisitos Físicos y Digitales del Manual
    public string? ArchivoEtiquetaJpgUri { get; set; }
    public bool EtiquetasFisicas2x4Entregadas { get; set; } // 20 etiquetas físicas de 2"x4"
    public bool TimbreL50Pagado { get; set; }               // Timbre de contratación de L 50.00
    public bool TasasOficialesPagadas { get; set; }

    // Responsables internos
    public string? ParalegalAsignado { get; set; }
    public string? AbogadoResponsable { get; set; }

    // Relaciones del Expediente
    public ICollection<ActuacionExpediente> Actuaciones { get; set; } = new List<ActuacionExpediente>();
    public ICollection<PublicacionENAG> Publicaciones { get; set; } = new List<PublicacionENAG>();
    public ICollection<Litigio> Litigios { get; set; } = new List<Litigio>();
    public ICollection<PlazoLegal> PlazosLegales { get; set; } = new List<PlazoLegal>();
    public ICollection<DocumentoExpediente> Documentos { get; set; } = new List<DocumentoExpediente>();
    public CertificadoRegistro? Certificado { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }
}
