using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio central de notificaciones internas del sistema LegalTech.
/// Soporta segmentación por rol, persistencia en base de datos y eventos reactivos para la UI.
/// </summary>
public class NotificacionesAppService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    /// <summary>
    /// Evento reactivo que se dispara cuando se crea una nueva notificación con el payload completo,
    /// permitiendo a los componentes de UI mostrar Toasts emergentes o alertar al usuario de inmediato.
    /// </summary>
    public event Func<NotificacionSistema, Task>? OnNuevaNotificacion;

    /// <summary>
    /// Evento reactivo que se dispara cuando se crea o actualiza una notificación,
    /// permitiendo a componentes como la campana en MainLayout refrescar su contador sin recargar la página.
    /// </summary>
    public event Func<Task>? OnNotificacionesActualizadas;

    public NotificacionesAppService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Difunde de manera segura las actualizaciones a todos los circuitos de Blazor Server conectados.
    /// Si un circuito se desconecta, se aísla el error para no interrumpir a los demás usuarios.
    /// </summary>
    private async Task EmitirCambioAsync(NotificacionSistema? nueva = null)
    {
        if (nueva != null && OnNuevaNotificacion != null)
        {
            var delegados = OnNuevaNotificacion.GetInvocationList();
            foreach (var d in delegados)
            {
                try
                {
                    if (d is Func<NotificacionSistema, Task> asyncFunc)
                    {
                        await asyncFunc(nueva);
                    }
                }
                catch
                {
                    // Circuito desconectado o en proceso de cierre
                }
            }
        }

        if (OnNotificacionesActualizadas != null)
        {
            var delegados = OnNotificacionesActualizadas.GetInvocationList();
            foreach (var d in delegados)
            {
                try
                {
                    if (d is Func<Task> asyncFunc)
                    {
                        await asyncFunc();
                    }
                }
                catch
                {
                    // Circuito desconectado o en proceso de cierre
                }
            }
        }
    }

    /// <summary>
    /// Registra una nueva notificación en la base de datos y notifica a los suscriptores activos.
    /// </summary>
    public async Task CrearNotificacionAsync(NotificacionSistema notificacion)
    {
        using var db = await _factory.CreateDbContextAsync();
        db.Notificaciones.Add(notificacion);
        await db.SaveChangesAsync();

        await EmitirCambioAsync(notificacion);
    }

    /// <summary>
    /// Obtiene las notificaciones dirigidas al usuario o a su rol de seguridad.
    /// </summary>
    public async Task<List<NotificacionSistema>> ObtenerNotificacionesUsuarioAsync(Guid? usuarioId, string? rol, int limite = 25)
    {
        using var db = await _factory.CreateDbContextAsync();
        var query = db.Notificaciones.AsNoTracking().AsQueryable();

        query = query.Where(n =>
            (usuarioId.HasValue && n.UsuarioDestinoId == usuarioId.Value) ||
            (!string.IsNullOrEmpty(rol) && n.RolDestino != null && (
                n.RolDestino.Contains(rol) || 
                (rol == RolesSistema.SocioDirector && n.RolDestino.Contains(RolesSistema.Finanzas))
            )) ||
            (n.UsuarioDestinoId == null && string.IsNullOrEmpty(n.RolDestino))
        );

        return await query
            .OrderByDescending(n => n.FechaCreacion)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Cuenta las notificaciones pendientes de lectura para el usuario o su rol.
    /// </summary>
    public async Task<int> ContarNoLeidasAsync(Guid? usuarioId, string? rol)
    {
        using var db = await _factory.CreateDbContextAsync();
        var query = db.Notificaciones.AsNoTracking().Where(n => !n.Leida);

        query = query.Where(n =>
            (usuarioId.HasValue && n.UsuarioDestinoId == usuarioId.Value) ||
            (!string.IsNullOrEmpty(rol) && n.RolDestino != null && (
                n.RolDestino.Contains(rol) || 
                (rol == RolesSistema.SocioDirector && n.RolDestino.Contains(RolesSistema.Finanzas))
            )) ||
            (n.UsuarioDestinoId == null && string.IsNullOrEmpty(n.RolDestino))
        );

        return await query.CountAsync();
    }

    /// <summary>
    /// Marca una notificación específica como leída.
    /// </summary>
    public async Task MarcarComoLeidaAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var notif = await db.Notificaciones.FindAsync(id);
        if (notif != null && !notif.Leida)
        {
            notif.Leida = true;
            await db.SaveChangesAsync();

            await EmitirCambioAsync();
        }
    }

    /// <summary>
    /// Marca todas las notificaciones pendientes del usuario/rol como leídas.
    /// </summary>
    public async Task MarcarTodasComoLeidasAsync(Guid? usuarioId, string? rol)
    {
        using var db = await _factory.CreateDbContextAsync();
        var query = db.Notificaciones.Where(n => !n.Leida);

        query = query.Where(n =>
            (usuarioId.HasValue && n.UsuarioDestinoId == usuarioId.Value) ||
            (!string.IsNullOrEmpty(rol) && n.RolDestino != null && (
                n.RolDestino.Contains(rol) || 
                (rol == RolesSistema.SocioDirector && n.RolDestino.Contains(RolesSistema.Finanzas))
            )) ||
            (n.UsuarioDestinoId == null && string.IsNullOrEmpty(n.RolDestino))
        );

        var pendientes = await query.ToListAsync();
        if (pendientes.Any())
        {
            foreach (var n in pendientes)
            {
                n.Leida = true;
            }
            await db.SaveChangesAsync();

            await EmitirCambioAsync();
        }
    }
}
