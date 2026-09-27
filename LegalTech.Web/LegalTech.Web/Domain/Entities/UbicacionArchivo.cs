namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo parametrizable de Ubicaciones Físicas de Archivo y Custodia Documental del bufete.
/// </summary>
public class UbicacionArchivo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: Archivo Activo - Trámites, Bóveda de Títulos Originales

    public string? CodigoArea { get; set; }             // Ej: GAV-01, BOV-02, EST-03

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
