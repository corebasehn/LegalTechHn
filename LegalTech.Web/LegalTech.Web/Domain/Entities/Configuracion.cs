using System;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Tabla genérica de configuraciones y parámetros del sistema (Llave-Valor).
/// Permite parametrizar rutas de almacenamiento (Local vs Synology NAS), credenciales y ajustes sin código en duro.
/// </summary>
public class Configuracion
{
    public int IdConfiguracion { get; set; }
    public string Llave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "Almacenamiento"; // Almacenamiento, Integraciones, General
    public bool EsSensible { get; set; } // true para contraseñas/tokens (ocultar en UI)
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
