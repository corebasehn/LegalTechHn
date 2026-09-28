using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio de gestión documental y repositorio digital de expedientes marcarios (MOD-06).
/// Resuelve la ruta de almacenamiento según la tabla de configuraciones (Local vs Synology NAS).
/// </summary>
public class DocumentoService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;
    private readonly ConfiguracionService _configService;
    private readonly IWebHostEnvironment _env;

    private const long MaxTamanioArchivo = 25 * 1024 * 1024; // 25 MB

    public DocumentoService(
        IDbContextFactory<LegalTechDbContext> factory,
        ConfiguracionService configService,
        IWebHostEnvironment env)
    {
        _factory = factory;
        _configService = configService;
        _env = env;
    }

    /// <summary>
    /// Lista todos los documentos custodiados para un expediente marcario.
    /// </summary>
    public async Task<List<DocumentoExpediente>> GetDocumentosPorExpedienteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.DocumentosExpediente
            .Where(d => d.ExpedienteId == expedienteId)
            .OrderByDescending(d => d.FechaSubida)
            .ToListAsync();
    }

    /// <summary>
    /// Sube y archiva un documento en el repositorio digital del expediente a partir de un flujo Stream.
    /// </summary>
    public async Task<DocumentoExpediente> SubirYGuardarDocumentoAsync(
        Guid expedienteId,
        Stream archivoStream,
        string nombreOriginal,
        string tipoMime,
        long tamanoBytes,
        string categoria,
        string tipoDocumento,
        string? observaciones,
        bool custodiaFisicaVerificada,
        string? ubicacionFisica,
        string subidoPor = "Usuario")
    {
        using var db = await _factory.CreateDbContextAsync();

        var expediente = await db.Expedientes.FindAsync(expedienteId)
            ?? throw new InvalidOperationException("Expediente no encontrado.");

        string tipoAlmacenamiento = await _configService.GetValorAsync("TipoAlmacenamiento", "LOCAL");
        string rutaLocalParam = await _configService.GetValorAsync("RutaLocal", "uploads/expedientes");
        string rutaSynologyParam = await _configService.GetValorAsync("RutaSynology", "https://nas.legaltech.hn/marcas");

        string webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        string carpetaExpediente = Path.Combine(webRoot, rutaLocalParam.Replace('/', Path.DirectorySeparatorChar), expedienteId.ToString());

        if (!Directory.Exists(carpetaExpediente))
        {
            Directory.CreateDirectory(carpetaExpediente);
        }

        string extensionOriginal = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        string nombreSanitizado = SanitizarNombreArchivo(Path.GetFileNameWithoutExtension(nombreOriginal));
        string nombreArchivoFinal = $"{Guid.NewGuid():N}_{nombreSanitizado}{extensionOriginal}";
        string rutaFisicaCompleta = Path.Combine(carpetaExpediente, nombreArchivoFinal);

        // Guardar físicamente el flujo del archivo
        await using (var fileStream = new FileStream(rutaFisicaCompleta, FileMode.Create, FileAccess.Write))
        {
            await archivoStream.CopyToAsync(fileStream);
        }

        string rutaRelativaWeb = $"/{rutaLocalParam.Trim('/')}/{expedienteId}/{nombreArchivoFinal}";
        string rutaAlmacenamientoFinal = tipoAlmacenamiento.ToUpperInvariant() == "SYNOLOGY"
            ? $"{rutaSynologyParam.TrimEnd('/')}/{expedienteId}/{nombreArchivoFinal}"
            : rutaRelativaWeb;

        var nuevoDoc = new DocumentoExpediente
        {
            Id = Guid.NewGuid(),
            ExpedienteId = expedienteId,
            Categoria = categoria,
            TipoDocumento = tipoDocumento,
            NombreOriginal = nombreOriginal,
            RutaAlmacenamiento = rutaAlmacenamientoFinal,
            Extension = extensionOriginal,
            TipoMime = tipoMime,
            TamanoBytes = tamanoBytes,
            Observaciones = observaciones,
            ProveedorAlmacenamiento = tipoAlmacenamiento.ToUpperInvariant(),
            CustodiaFisicaVerificada = custodiaFisicaVerificada,
            UbicacionFisica = ubicacionFisica,
            FechaSubida = DateTime.UtcNow,
            SubidoPor = subidoPor
        };

        db.DocumentosExpediente.Add(nuevoDoc);

        // Sincronización automática con metadatos del expediente según el tipo de documento
        if (tipoDocumento.Contains("Logotipo", StringComparison.OrdinalIgnoreCase) || 
            tipoDocumento.Contains("Etiqueta", StringComparison.OrdinalIgnoreCase))
        {
            expediente.ArchivoEtiquetaJpgUri = rutaRelativaWeb;
        }
        else if (tipoDocumento.Contains("Carátula", StringComparison.OrdinalIgnoreCase) || 
                 tipoDocumento.Contains("Comprobante de Presentación", StringComparison.OrdinalIgnoreCase))
        {
            expediente.ComprobanteRecepcionUri = rutaRelativaWeb;
        }

        await db.SaveChangesAsync();
        return nuevoDoc;
    }

    /// <summary>
    /// Sobrecarga para subir directamente desde IBrowserFile.
    /// </summary>
    public async Task<DocumentoExpediente> SubirDocumentoAsync(
        Guid expedienteId,
        IBrowserFile archivo,
        string categoria,
        string tipoDocumento,
        string? observaciones,
        bool custodiaFisica,
        string? ubicacionFisica,
        string usuario = "Usuario")
    {
        using var stream = archivo.OpenReadStream(MaxTamanioArchivo);
        return await SubirYGuardarDocumentoAsync(
            expedienteId,
            stream,
            archivo.Name,
            archivo.ContentType,
            archivo.Size,
            categoria,
            tipoDocumento,
            observaciones,
            custodiaFisica,
            ubicacionFisica,
            usuario);
    }

    /// <summary>
    /// Elimina un documento tanto de la base de datos como del disco físico.
    /// </summary>
    public async Task<bool> EliminarDocumentoAsync(Guid documentoId)
    {
        using var db = await _factory.CreateDbContextAsync();
        var doc = await db.DocumentosExpediente.FindAsync(documentoId);
        if (doc != null)
        {
            // Intentar eliminar del disco si es almacenamiento local
            if (doc.RutaAlmacenamiento.StartsWith("/"))
            {
                string webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
                string rutaFisica = Path.Combine(webRoot, doc.RutaAlmacenamiento.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(rutaFisica))
                {
                    try { File.Delete(rutaFisica); } catch { /* Ignore file locked */ }
                }
            }

            db.DocumentosExpediente.Remove(doc);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Actualiza el estado de custodia física de un documento en el archivo central.
    /// </summary>
    public async Task ActualizarCustodiaFisicaAsync(Guid documentoId, bool custodiado, string? ubicacion)
    {
        using var db = await _factory.CreateDbContextAsync();
        var doc = await db.DocumentosExpediente.FindAsync(documentoId);
        if (doc != null)
        {
            doc.CustodiaFisicaVerificada = custodiado;
            doc.UbicacionFisica = ubicacion;
            await db.SaveChangesAsync();
        }
    }

    private static string SanitizarNombreArchivo(string nombre)
    {
        string sinEspeciales = Regex.Replace(nombre, @"[^a-zA-Z0-9_\-]", "_");
        return sinEspeciales.Length > 40 ? sinEspeciales.Substring(0, 40) : sinEspeciales;
    }
}
