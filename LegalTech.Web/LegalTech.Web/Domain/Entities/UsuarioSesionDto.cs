using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// DTO con los datos de la sesión activa del usuario para el AuthenticationStateProvider.
/// </summary>
public class UsuarioSesionDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string TokenSesion { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; } = DateTime.UtcNow.AddHours(8);
}
