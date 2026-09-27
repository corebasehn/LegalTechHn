namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo parametrizable de Tipos de Actuaciones e Hitos Procesales para la Línea de Tiempo.
/// </summary>
public class TipoActuacion
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: Auto de Prevención, Publicación La Gaceta, Oposición

    public string Icono { get; set; } = "event";        // Ícono de Radzen/Material (description, g_mobiledata, report_problem)

    public string Color { get; set; } = "primary";      // Color visual para la línea de tiempo

    public string? Descripcion { get; set; }

    public bool RequiereDocumentoAdjunto { get; set; }

    public bool Activo { get; set; } = true;
}
