namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo parametrizable de Estados Procesales del trámite marcario ante la DIGEPIH.
/// Permite al bufete agregar o personalizar estados directamente en la Base de Datos.
/// </summary>
public class EstadoProcesal
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: Recepción, Examen de Forma, Con Objeción de Fondo, Concedida

    public string Fase { get; set; } = "Apertura";      // Apertura, Examen, Publicación, Oposición, Concesión, Vigilancia

    public string ColorBadge { get; set; } = "primary"; // primary, success, warning, danger, info, secondary

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}
