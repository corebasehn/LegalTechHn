using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio para la administración dinámica de tablas paramétricas y catálogos en base de datos.
/// Permite al bufete agregar, modificar o desactivar opciones sin recompilar código.
/// </summary>
public class CatalogoService
{
    private readonly IDbContextFactory<LegalTechDbContext> _contextFactory;

    public CatalogoService(IDbContextFactory<LegalTechDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // =========================================================================
    // 1. TIPOS DE SIGNOS DISTINTIVOS (TiposSigno)
    // =========================================================================
    public async Task<List<TipoSigno>> GetTiposSignoAsync(bool soloActivos = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TiposSigno.AsQueryable();
        if (soloActivos) query = query.Where(t => t.Activo);
        return await query.OrderBy(t => t.Id).ToListAsync();
    }

    public async Task GuardarTipoSignoAsync(TipoSigno tipo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (tipo.Id == 0)
        {
            context.TiposSigno.Add(tipo);
        }
        else
        {
            var existente = await context.TiposSigno.FindAsync(tipo.Id);
            if (existente != null)
            {
                existente.Nombre = tipo.Nombre;
                existente.Descripcion = tipo.Descripcion;
                existente.Activo = tipo.Activo;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ToggleTipoSignoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.TiposSigno.FindAsync(id);
        if (item != null)
        {
            item.Activo = !item.Activo;
            await context.SaveChangesAsync();
        }
    }

    // =========================================================================
    // 2. ESTADOS PROCESALES (EstadosProcesales)
    // =========================================================================
    public async Task<List<EstadoProcesal>> GetEstadosProcesalesAsync(bool soloActivos = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.EstadosProcesales.AsQueryable();
        if (soloActivos) query = query.Where(e => e.Activo);
        return await query.OrderBy(e => e.Orden).ThenBy(e => e.Id).ToListAsync();
    }

    public async Task GuardarEstadoProcesalAsync(EstadoProcesal estado)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (estado.Id == 0)
        {
            if (estado.Orden == 0)
            {
                var maxOrden = await context.EstadosProcesales.MaxAsync(e => (int?)e.Orden) ?? 0;
                estado.Orden = maxOrden + 1;
            }
            context.EstadosProcesales.Add(estado);
        }
        else
        {
            var existente = await context.EstadosProcesales.FindAsync(estado.Id);
            if (existente != null)
            {
                existente.Nombre = estado.Nombre;
                existente.Fase = estado.Fase;
                existente.ColorBadge = estado.ColorBadge;
                existente.Orden = estado.Orden;
                existente.Activo = estado.Activo;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ToggleEstadoProcesalAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.EstadosProcesales.FindAsync(id);
        if (item != null)
        {
            item.Activo = !item.Activo;
            await context.SaveChangesAsync();
        }
    }

    // =========================================================================
    // 3. TIPOS DE ACTUACIONES / HITOS (TiposActuaciones)
    // =========================================================================
    public async Task<List<TipoActuacion>> GetTiposActuacionesAsync(bool soloActivos = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TiposActuaciones.AsQueryable();
        if (soloActivos) query = query.Where(ta => ta.Activo);
        return await query.OrderBy(ta => ta.Id).ToListAsync();
    }

    public async Task GuardarTipoActuacionAsync(TipoActuacion actuacion)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (actuacion.Id == 0)
        {
            context.TiposActuaciones.Add(actuacion);
        }
        else
        {
            var existente = await context.TiposActuaciones.FindAsync(actuacion.Id);
            if (existente != null)
            {
                existente.Nombre = actuacion.Nombre;
                existente.Icono = actuacion.Icono;
                existente.Color = actuacion.Color;
                existente.Descripcion = actuacion.Descripcion;
                existente.RequiereDocumentoAdjunto = actuacion.RequiereDocumentoAdjunto;
                existente.Activo = actuacion.Activo;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ToggleTipoActuacionAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.TiposActuaciones.FindAsync(id);
        if (item != null)
        {
            item.Activo = !item.Activo;
            await context.SaveChangesAsync();
        }
    }

    // =========================================================================
    // 4. VÍAS DE PRESENTACIÓN DIGEPIH (ViasPresentacion)
    // =========================================================================
    public async Task<List<ViaPresentacion>> GetViasPresentacionAsync(bool soloActivos = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.ViasPresentacion.AsQueryable();
        if (soloActivos) query = query.Where(vp => vp.Activo);
        return await query.OrderBy(vp => vp.Id).ToListAsync();
    }

    public async Task GuardarViaPresentacionAsync(ViaPresentacion via)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (via.Id == 0)
        {
            context.ViasPresentacion.Add(via);
        }
        else
        {
            var existente = await context.ViasPresentacion.FindAsync(via.Id);
            if (existente != null)
            {
                existente.Nombre = via.Nombre;
                existente.Descripcion = via.Descripcion;
                existente.Activo = via.Activo;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ToggleViaPresentacionAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.ViasPresentacion.FindAsync(id);
        if (item != null)
        {
            item.Activo = !item.Activo;
            await context.SaveChangesAsync();
        }
    }

    // =========================================================================
    // 5. UBICACIONES FÍSICAS DE ARCHIVO / BÓVEDA (UbicacionesArchivo)
    // =========================================================================
    public async Task<List<UbicacionArchivo>> GetUbicacionesArchivoAsync(bool soloActivos = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.UbicacionesArchivo.AsQueryable();
        if (soloActivos) query = query.Where(ua => ua.Activo);
        return await query.OrderBy(ua => ua.Id).ToListAsync();
    }

    public async Task GuardarUbicacionArchivoAsync(UbicacionArchivo ubicacion)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (ubicacion.Id == 0)
        {
            context.UbicacionesArchivo.Add(ubicacion);
        }
        else
        {
            var existente = await context.UbicacionesArchivo.FindAsync(ubicacion.Id);
            if (existente != null)
            {
                existente.Nombre = ubicacion.Nombre;
                existente.CodigoArea = ubicacion.CodigoArea;
                existente.Descripcion = ubicacion.Descripcion;
                existente.Activo = ubicacion.Activo;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ToggleUbicacionArchivoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.UbicacionesArchivo.FindAsync(id);
        if (item != null)
        {
            item.Activo = !item.Activo;
            await context.SaveChangesAsync();
        }
    }
}
