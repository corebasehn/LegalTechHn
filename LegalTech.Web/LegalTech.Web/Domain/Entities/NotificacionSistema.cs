using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Representa una notificación interna del sistema LegalTech.
/// Dirigida a un usuario específico o a todos los usuarios con un rol determinado (ej. Finanzas, SocioDirector).
/// </summary>
public class NotificacionSistema
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID del usuario destinatario (si es una notificación personal).
    /// </summary>
    public Guid? UsuarioDestinoId { get; set; }

    /// <summary>
    /// Rol o roles destinatarios separados por coma (ej. "Finanzas,SocioDirector" o "AbogadoSenior").
    /// Si es null y UsuarioDestinoId es null, es una notificación global del sistema.
    /// </summary>
    public string? RolDestino { get; set; }

    /// <summary>
    /// Título corto de la notificación.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje o detalle explicativo de la notificación.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación: "Info", "Warning", "Success", "Danger".
    /// </summary>
    public string Tipo { get; set; } = "Info";

    /// <summary>
    /// Icono Radzen a mostrar (ej. "receipt_long", "verified", "undo", "warning").
    /// </summary>
    public string Icono { get; set; } = "notifications";

    /// <summary>
    /// Ruta interna a la cual dirigir al usuario al hacer clic (ej. "/facturacion").
    /// </summary>
    public string? UrlDestino { get; set; }

    /// <summary>
    /// Indica si el usuario ya vio o abrió la notificación.
    /// </summary>
    public bool Leida { get; set; } = false;

    /// <summary>
    /// Fecha y hora UTC en que se generó la notificación.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nombre del usuario o sistema que disparó la notificación.
    /// </summary>
    public string? CreadoPor { get; set; }
}
