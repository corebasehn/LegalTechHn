namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Parámetro configurable fiscal, arancelario o regulatorio en Honduras (cero datos en duro en código).
/// Permite administrar tasas de cambio, ISV, timbres del colegio de abogados, tasas DIGEPIH y rangos SAR.
/// </summary>
public class ParametroFiscal
{
    public int Id { get; set; }

    /// <summary>
    /// Clave única del parámetro (ej: ISV_PORCENTAJE, TASA_CAMBIO_USD_HNL, TIMBRE_CONTRATACION_HNL)
    /// </summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo para mostrar al usuario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Valor almacenado como texto configurable (parseable a decimal, fecha o texto).
    /// </summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dato: "decimal", "string", "integer", "date"
    /// </summary>
    public string TipoDato { get; set; } = "decimal";

    /// <summary>
    /// Categoría para agrupación (ej: "Impuestos", "Tasas Oficiales", "Divisas", "Facturación SAR")
    /// </summary>
    public string Categoria { get; set; } = "General";

    /// <summary>
    /// Explicación legal o administrativa del parámetro.
    /// </summary>
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
