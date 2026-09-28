using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Documento digitalizado custodiado en el repositorio del expediente marcario (MOD-06).
/// Soporta almacenamiento local y servidor Synology NAS con trazabilidad de custodia física.
/// </summary>
public class DocumentoExpediente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExpedienteId { get; set; }
    public Expediente Expediente { get; set; } = null!;

    // Clasificación Documental
    public string Categoria { get; set; } = "Solicitud"; // Solicitud, Poderes, TasasTimbres, Gaceta, Resoluciones, Litigios, TituloConcesion, Otros
    public string TipoDocumento { get; set; } = string.Empty; // Ej: "Carátula Sellada DIGEPIH", "Logotipo Oficial", "Aviso 1 La Gaceta", "Título Original"

    // Metadatos del Archivo
    public string NombreOriginal { get; set; } = string.Empty;
    public string RutaAlmacenamiento { get; set; } = string.Empty; // Ruta relativa local o ID/Path en Synology
    public string Extension { get; set; } = string.Empty; // .pdf, .jpg, .png, .docx
    public string TipoMime { get; set; } = string.Empty; // application/pdf, image/png, etc.
    public long TamanoBytes { get; set; }
    public string? Observaciones { get; set; }

    // Proveedor de Almacenamiento utilizado al guardar
    public string ProveedorAlmacenamiento { get; set; } = "LOCAL"; // "LOCAL" o "SYNOLOGY"

    // Custodia Física en Archivo Central
    public bool CustodiaFisicaVerificada { get; set; }
    public string? UbicacionFisica { get; set; } // Ej: "Bóveda 1, Archivador A-3, Carpeta EXP-2026-0001"

    // Trazabilidad
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    public string SubidoPor { get; set; } = "Usuario";
}
