using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio de administración centralizada de la tabla de Configuraciones (Llave-Valor).
/// Garantiza cero código en duro para rutas de almacenamiento, credenciales del Synology NAS y parámetros globales.
/// </summary>
public class ConfiguracionService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    public ConfiguracionService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Configuracion>> GetConfiguracionesAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        await InicializarConfiguracionesBaseAsync(db);
        return await db.Configuraciones
            .OrderBy(c => c.Categoria)
            .ThenBy(c => c.Llave)
            .ToListAsync();
    }

    public async Task<string> GetValorAsync(string llave, string valorDefecto = "")
    {
        using var db = await _factory.CreateDbContextAsync();
        var item = await db.Configuraciones.FirstOrDefaultAsync(c => c.Llave == llave);
        if (item != null && !string.IsNullOrWhiteSpace(item.Valor))
        {
            return item.Valor;
        }
        return valorDefecto;
    }

    public async Task<Configuracion?> GetConfiguracionAsync(string llave)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Configuraciones.FirstOrDefaultAsync(c => c.Llave == llave);
    }

    public async Task ActualizarValorAsync(string llave, string nuevoValor)
    {
        using var db = await _factory.CreateDbContextAsync();
        var item = await db.Configuraciones.FirstOrDefaultAsync(c => c.Llave == llave);
        if (item != null)
        {
            item.Valor = nuevoValor;
            item.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        else
        {
            db.Configuraciones.Add(new Configuracion
            {
                Llave = llave,
                Valor = nuevoValor,
                ActualizadoEn = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task<Configuracion> GuardarConfiguracionAsync(Configuracion config)
    {
        using var db = await _factory.CreateDbContextAsync();
        if (config.IdConfiguracion == 0)
        {
            config.ActualizadoEn = DateTime.UtcNow;
            db.Configuraciones.Add(config);
        }
        else
        {
            var existente = await db.Configuraciones.FindAsync(config.IdConfiguracion)
                ?? throw new InvalidOperationException("Configuración no encontrada.");

            existente.Llave = config.Llave;
            existente.Valor = config.Valor;
            existente.Descripcion = config.Descripcion;
            existente.Categoria = config.Categoria;
            existente.EsSensible = config.EsSensible;
            existente.ActualizadoEn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return config;
    }

    public async Task InicializarConfiguracionesBaseAsync(LegalTechDbContext db)
    {
        var baseConfigs = new List<Configuracion>
        {
            new()
            {
                Llave = "RutaLocal",
                Valor = "uploads/expedientes",
                Categoria = "Almacenamiento",
                Descripcion = "Ruta relativa local en el servidor para almacenar los documentos de los expedientes",
                EsSensible = false
            },
            new()
            {
                Llave = "TipoAlmacenamiento",
                Valor = "LOCAL",
                Categoria = "Almacenamiento",
                Descripcion = "Proveedor de almacenamiento activo: 'LOCAL' (servidor) o 'SYNOLOGY' (NAS externo)",
                EsSensible = false
            },
            new()
            {
                Llave = "RutaSynology",
                Valor = "https://nas.legaltech.hn/marcas",
                Categoria = "Almacenamiento",
                Descripcion = "Ruta de red compartida (UNC) o URL externa del servidor Synology NAS",
                EsSensible = false
            },
            new()
            {
                Llave = "UsuarioSynology",
                Valor = "admin_marcas",
                Categoria = "Almacenamiento",
                Descripcion = "Usuario con permisos de lectura y escritura en el Synology NAS",
                EsSensible = false
            },
            new()
            {
                Llave = "ClaveSynology",
                Valor = "",
                Categoria = "Almacenamiento",
                Descripcion = "Contraseña o token de aplicación para autenticación en el Synology NAS",
                EsSensible = true
            }
        };

        bool cambios = false;
        foreach (var c in baseConfigs)
        {
            if (!await db.Configuraciones.AnyAsync(x => x.Llave == c.Llave))
            {
                c.ActualizadoEn = DateTime.UtcNow;
                db.Configuraciones.Add(c);
                cambios = true;
            }
        }

        if (cambios)
        {
            await db.SaveChangesAsync();
        }
    }
}
