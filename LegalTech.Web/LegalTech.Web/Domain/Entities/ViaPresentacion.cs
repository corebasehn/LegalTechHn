namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo parametrizable de Vías de Presentación Oficial ante el Instituto de la Propiedad (DIGEPIH).
/// </summary>
public class ViaPresentacion
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: Ventanilla Presencial Tegucigalpa, SPS, Portal en Línea

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
