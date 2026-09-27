namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo parametrizable de Tipos de Signos Distintivos (Tabla en Base de Datos).
/// Permite agregar nuevos tipos (ej: Marca Colectiva, Certificación, Denominación de Origen) sin tocar código.
/// </summary>
public class TipoSigno
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: Marca de Fábrica, Mixta, Colectiva, etc.

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
