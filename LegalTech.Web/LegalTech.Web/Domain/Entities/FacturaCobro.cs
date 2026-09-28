using System;
using System.Collections.Generic;
using LegalTech.Web.Domain.Enums;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Comprobante de facturación y cobranza por servicios de Propiedad Intelectual.
/// Base del cumplimiento de la Regla Infranqueable de Solvencia P360 (Manual de Marcas).
/// </summary>
public class FacturaCobro
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Número correlativo o fiscal (ej: FAC-2026-0001, 000-001-01-00045812)
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Código de Autorización de Impresión del Servicio de Administración de Rentas (SAR Honduras)
    /// </summary>
    public string? NumeroCAI { get; set; }

    // Cliente obligado al pago
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Expediente marcario asociado (opcional si es facturación corporativa consolidada)
    public Guid? ExpedienteId { get; set; }
    public Expediente? Expediente { get; set; }

    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddDays(15);

    /// <summary>
    /// Moneda de facturación: "HNL" o "USD"
    /// </summary>
    public string Moneda { get; set; } = "HNL";

    /// <summary>
    /// Tasa de cambio oficial aplicada si la factura es en dólares o se cobra en lempiras.
    /// </summary>
    public decimal TasaCambio { get; set; } = 25.50m;

    // Desglose contable
    public decimal SubtotalHonorarios { get; set; }
    public decimal SubtotalGastosOficiales { get; set; } // Tasas DIGEPIH, timbres y ENAG exentos
    public decimal MontoISV { get; set; }              // 15% sobre honorarios gravados
    public decimal TotalFactura { get; set; }

    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente { get; set; }

    public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;

    // Flujo de Aprobación y Proforma
    public string? SolicitadoPor { get; set; }
    public string? AprobadoPor { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? MotivoRechazo { get; set; }

    public bool EsProforma => Estado == EstadoFactura.Borrador || Estado == EstadoFactura.PendienteAprobacion || Estado == EstadoFactura.Rechazada;

    public DateTime? FechaPago { get; set; }
    public string? MetodoPago { get; set; }           // Transferencia Bancaria, Cheque, Tarjeta, etc.
    public string? ReferenciaBancaria { get; set; }   // No. de depósito o confirmación ACH
    public string? Observaciones { get; set; }

    /// <summary>
    /// Hito del proceso marcario facturado (ej: "Apertura y Búsqueda", "Presentación Oficial", "Publicaciones", "Título", "Litigio")
    /// </summary>
    public string? HitoProcesal { get; set; }

    public ICollection<DetalleFacturaCobro> Detalles { get; set; } = new List<DetalleFacturaCobro>();

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
