using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Usuario del sistema LegalTech con credenciales autenticadas, rol asignado y protección anti-fuerza bruta.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    // Seguridad Criptográfica (PBKDF2 HMAC-SHA256 con Salt aleatorio único)
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    // Rol del Sistema (SocioDirector, AbogadoSenior, Paralegal, Finanzas)
    public string Rol { get; set; } = RolesSistema.AbogadoSenior;

    // Cargo en el Despacho Legal
    public string Cargo { get; set; } = "Abogado";
    public string? Telefono { get; set; }

    // Control de Estado y Bloqueo Anti-Fuerza Bruta
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; } = 0;
    public DateTime? BloqueadoHasta { get; set; }

    // Trazabilidad
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
