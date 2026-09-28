using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Línea de detalle de una factura o estado de cobro de propiedad intelectual.
/// </summary>
public class DetalleFacturaCobro
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FacturaId { get; set; }
    public FacturaCobro Factura { get; set; } = null!;

    public int? TarifaArancelariaId { get; set; }
    public TarifaArancelaria? TarifaArancelaria { get; set; }

    public string Concepto { get; set; } = string.Empty;

    /// <summary>
    /// Distingue entre Gasto Oficial Gubernamental (Exento) y Honorario Profesional (Gravado con ISV 15%).
    /// </summary>
    public bool EsGastoOficial { get; set; }

    public int Cantidad { get; set; } = 1;

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public decimal MontoISV { get; set; }

    public decimal TotalLinea { get; set; }
}
