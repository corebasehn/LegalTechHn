namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo configurable de aranceles de honorarios y costos oficiales por etapa procesal en Honduras.
/// Permite al bufete ajustar precios en Lempiras y Dólares sin tocar código.
/// </summary>
public class TarifaArancelaria
{
    public int Id { get; set; }

    /// <summary>
    /// Concepto del servicio (ej: "Búsqueda Registral Previa", "Solicitud Ordinaria DIGEPIH", "Objeción de Fondo", etc.)
    /// </summary>
    public string Concepto { get; set; } = string.Empty;

    /// <summary>
    /// Etapa procesal a la que pertenece (Apertura, Presentación, Examen, Publicación, Concesión, Litigios)
    /// </summary>
    public string EtapaProcesal { get; set; } = "Presentación";

    /// <summary>
    /// Honorarios profesionales del bufete en Lempiras (HNL).
    /// </summary>
    public decimal HonorariosHNL { get; set; }

    /// <summary>
    /// Honorarios profesionales en Dólares (USD).
    /// </summary>
    public decimal HonorariosUSD { get; set; }

    /// <summary>
    /// Gastos oficiales no gravados (tasas gubernamentales DIGEPIH, timbres de contratación, ENAG) en HNL.
    /// </summary>
    public decimal GastosOficialesHNL { get; set; }

    /// <summary>
    /// Gastos oficiales en USD.
    /// </summary>
    public decimal GastosOficialesUSD { get; set; }

    /// <summary>
    /// Si aplica Impuesto sobre Ventas (ISV 15%) sobre el monto de honorarios.
    /// (Los gastos oficiales son siempre exentos según legislación hondureña).
    /// </summary>
    public bool AplicaISV { get; set; } = true;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
