using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Bitácora inmutable de accesos, intentos de autenticación y eventos de seguridad.
/// </summary>
public class AuditoriaAcceso
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UsuarioId { get; set; }
    public string EmailIngresado { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public bool Exitoso { get; set; }

    public string? IpDireccion { get; set; }
    public string? Navegador { get; set; }

    public string Detalle { get; set; } = string.Empty; // "Inicio de sesión exitoso", "Contraseña incorrecta", "Usuario inactivo", "Cuenta bloqueada"
}
