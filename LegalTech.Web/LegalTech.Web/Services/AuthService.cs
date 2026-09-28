using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;
using LegalTech.Web.Infrastructure.Security;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio de autenticación, autorización y administración de usuarios para LegalTech Honduras.
/// Implementa políticas anti-fuerza bruta, hashing criptográfico y bitácora de auditoría inmutable.
/// </summary>
public class AuthService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    public AuthService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida credenciales contra la base de datos aplicando control de bloqueo y registro en auditoría.
    /// </summary>
    public async Task<(bool Exitoso, string? Error, UsuarioSesionDto? Sesion)> ValidarCredencialesAsync(
        string identificador, string password, string? ip = null, string? navegador = null)
    {
        if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Debe ingresar su correo o usuario y la contraseña.", null);
        }

        using var db = await _factory.CreateDbContextAsync();
        string loginLimpio = identificador.Trim().ToLowerInvariant();

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == loginLimpio || u.Username.ToLower() == loginLimpio);

        if (usuario == null)
        {
            db.AuditoriasAcceso.Add(new AuditoriaAcceso
            {
                EmailIngresado = identificador,
                Exitoso = false,
                IpDireccion = ip,
                Navegador = navegador,
                Detalle = "Intento de inicio de sesión con usuario inexistente"
            });
            await db.SaveChangesAsync();
            return (false, "Credenciales incorrectas.", null);
        }

        // 1. Verificar si la cuenta está desactivada administrativamente
        if (!usuario.Activo)
        {
            db.AuditoriasAcceso.Add(new AuditoriaAcceso
            {
                UsuarioId = usuario.Id,
                EmailIngresado = usuario.Email,
                Exitoso = false,
                IpDireccion = ip,
                Navegador = navegador,
                Detalle = "Intento de acceso a cuenta inactiva o deshabilitada"
            });
            await db.SaveChangesAsync();
            return (false, "Su cuenta se encuentra desactivada. Contacte al Socio Director.", null);
        }

        // 2. Verificar política de bloqueo temporal por fuerza bruta
        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            var minutosRestantes = Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.UtcNow).TotalMinutes);
            db.AuditoriasAcceso.Add(new AuditoriaAcceso
            {
                UsuarioId = usuario.Id,
                EmailIngresado = usuario.Email,
                Exitoso = false,
                IpDireccion = ip,
                Navegador = navegador,
                Detalle = $"Acceso denegado: cuenta temporalmente bloqueada por fuerza bruta ({minutosRestantes} min restantes)"
            });
            await db.SaveChangesAsync();
            return (false, $"Cuenta bloqueada por múltiples intentos fallidos. Intente nuevamente en {minutosRestantes} minuto(s).", null);
        }

        // 3. Verificar hash criptográfico en tiempo constante
        bool passwordValida = PasswordHasher.VerificarPassword(password, usuario.PasswordHash, usuario.Salt);

        if (!passwordValida)
        {
            usuario.IntentosFallidos++;
            string detalle = $"Contraseña incorrecta (Intento {usuario.IntentosFallidos} de 5)";

            if (usuario.IntentosFallidos >= 5)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                detalle = "Cuenta bloqueada durante 15 minutos por alcanzar 5 intentos fallidos consecutivos";
            }

            db.AuditoriasAcceso.Add(new AuditoriaAcceso
            {
                UsuarioId = usuario.Id,
                EmailIngresado = usuario.Email,
                Exitoso = false,
                IpDireccion = ip,
                Navegador = navegador,
                Detalle = detalle
            });

            await db.SaveChangesAsync();

            if (usuario.IntentosFallidos >= 5)
            {
                return (false, "Ha excedido el número máximo de intentos permitidos. Su cuenta ha sido bloqueada por 15 minutos.", null);
            }

            return (false, $"Credenciales incorrectas. Intento {usuario.IntentosFallidos} de 5.", null);
        }

        // 4. Autenticación Exitosa: Reset de intentos y registro de auditoría
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = DateTime.UtcNow;

        db.AuditoriasAcceso.Add(new AuditoriaAcceso
        {
            UsuarioId = usuario.Id,
            EmailIngresado = usuario.Email,
            Exitoso = true,
            IpDireccion = ip,
            Navegador = navegador,
            Detalle = $"Inicio de sesión exitoso como {usuario.Rol}"
        });

        await db.SaveChangesAsync();

        var sesion = new UsuarioSesionDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Username = usuario.Username,
            Rol = usuario.Rol,
            Cargo = usuario.Cargo,
            TokenSesion = Guid.NewGuid().ToString("N"),
            FechaExpiracion = DateTime.UtcNow.AddHours(12)
        };

        return (true, null, sesion);
    }

    /// <summary>
    /// Lista todos los usuarios registrados en el sistema.
    /// </summary>
    public async Task<List<Usuario>> GetUsuariosAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Usuarios
            .OrderBy(u => u.Rol)
            .ThenBy(u => u.NombreCompleto)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    public async Task<Usuario?> GetUsuarioPorIdAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Usuarios.FindAsync(id);
    }

    /// <summary>
    /// Guarda o actualiza un usuario con validaciones de unicidad.
    /// </summary>
    public async Task<(bool Exitoso, string? Error)> GuardarUsuarioAsync(Usuario usuario, string? passwordNueva = null)
    {
        using var db = await _factory.CreateDbContextAsync();

        string emailNormalizado = usuario.Email.Trim().ToLowerInvariant();
        string usernameNormalizado = usuario.Username.Trim().ToLowerInvariant();

        // Validar unicidad de email
        bool emailExiste = await db.Usuarios.AnyAsync(u => u.Email.ToLower() == emailNormalizado && u.Id != usuario.Id);
        if (emailExiste)
        {
            return (false, $"El correo '{usuario.Email}' ya está asignado a otro usuario.");
        }

        // Validar unicidad de username
        bool usernameExiste = await db.Usuarios.AnyAsync(u => u.Username.ToLower() == usernameNormalizado && u.Id != usuario.Id);
        if (usernameExiste)
        {
            return (false, $"El nombre de usuario '{usuario.Username}' ya está en uso.");
        }

        if (usuario.Id == Guid.Empty || !await db.Usuarios.AnyAsync(u => u.Id == usuario.Id))
        {
            // Nuevo Usuario
            if (string.IsNullOrWhiteSpace(passwordNueva))
            {
                return (false, "Debe ingresar una contraseña para el nuevo usuario.");
            }

            usuario.Id = Guid.NewGuid();
            usuario.Salt = PasswordHasher.GenerarSalt();
            usuario.PasswordHash = PasswordHasher.HashPassword(passwordNueva, usuario.Salt);
            usuario.CreadoEn = DateTime.UtcNow;
            usuario.ActualizadoEn = DateTime.UtcNow;

            db.Usuarios.Add(usuario);
        }
        else
        {
            // Edición de Usuario Existente
            var existente = await db.Usuarios.FindAsync(usuario.Id);
            if (existente == null) return (false, "Usuario no encontrado.");

            existente.NombreCompleto = usuario.NombreCompleto;
            existente.Email = usuario.Email;
            existente.Username = usuario.Username;
            existente.Rol = usuario.Rol;
            existente.Cargo = usuario.Cargo;
            existente.Telefono = usuario.Telefono;
            existente.Activo = usuario.Activo;
            existente.ActualizadoEn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(passwordNueva))
            {
                existente.Salt = PasswordHasher.GenerarSalt();
                existente.PasswordHash = PasswordHasher.HashPassword(passwordNueva, existente.Salt);
            }
        }

        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Cambia la contraseña de un usuario validando la contraseña actual.
    /// </summary>
    public async Task<(bool Exitoso, string? Error)> CambiarPasswordAsync(Guid usuarioId, string passwordActual, string passwordNueva)
    {
        if (string.IsNullOrWhiteSpace(passwordNueva) || passwordNueva.Length < 6)
        {
            return (false, "La nueva contraseña debe tener al menos 6 caracteres.");
        }

        using var db = await _factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return (false, "Usuario no encontrado.");

        if (!PasswordHasher.VerificarPassword(passwordActual, usuario.PasswordHash, usuario.Salt))
        {
            return (false, "La contraseña actual no es correcta.");
        }

        usuario.Salt = PasswordHasher.GenerarSalt();
        usuario.PasswordHash = PasswordHasher.HashPassword(passwordNueva, usuario.Salt);
        usuario.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Permite al Socio Director resetear la contraseña de un usuario directamente.
    /// </summary>
    public async Task<(bool Exitoso, string? Error)> ResetearPasswordAdminAsync(Guid usuarioId, string nuevaPassword)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
        {
            return (false, "La nueva contraseña debe tener al menos 6 caracteres.");
        }

        using var db = await _factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return (false, "Usuario no encontrado.");

        usuario.Salt = PasswordHasher.GenerarSalt();
        usuario.PasswordHash = PasswordHasher.HashPassword(nuevaPassword, usuario.Salt);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Alterna el estado activo/desactivado de un usuario.
    /// </summary>
    public async Task ToggleActivoUsuarioAsync(Guid usuarioId)
    {
        using var db = await _factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario != null)
        {
            usuario.Activo = !usuario.Activo;
            usuario.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obtiene las entradas más recientes de la bitácora de auditoría de accesos.
    /// </summary>
    public async Task<List<AuditoriaAcceso>> GetAuditoriasAccesosAsync(int top = 50)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.AuditoriasAcceso
            .OrderByDescending(a => a.Fecha)
            .Take(top)
            .ToListAsync();
    }

    /// <summary>
    /// Inicializa los usuarios semilla de base si la tabla está vacía.
    /// </summary>
    public async Task InicializarUsuariosBaseAsync(LegalTechDbContext db)
    {
        if (!await db.Usuarios.AnyAsync())
        {
            var usuariosBase = new List<(string Nombre, string Email, string Username, string Password, string Rol, string Cargo)>
            {
                ("Lic. Roberto Morales (Socio Director)", "admin@legaltech.hn", "admin", "Admin2026!", RolesSistema.SocioDirector, "Socio Director y Fundador"),
                ("Abog. Carlos Mendoza", "abogado@legaltech.hn", "abogado", "Abogado2026!", RolesSistema.AbogadoSenior, "Abogado Senior de Propiedad Intelectual"),
                ("Licda. Elena Torres", "paralegal@legaltech.hn", "paralegal", "Paralegal2026!", RolesSistema.Paralegal, "Procuradora y Paralegal DIGEPIH"),
                ("Licda. María Fernández", "finanzas@legaltech.hn", "finanzas", "Finanzas2026!", RolesSistema.Finanzas, "Gerente de Facturación SAR y Finanzas")
            };

            foreach (var item in usuariosBase)
            {
                string salt = PasswordHasher.GenerarSalt();
                string hash = PasswordHasher.HashPassword(item.Password, salt);

                db.Usuarios.Add(new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = item.Nombre,
                    Email = item.Email,
                    Username = item.Username,
                    PasswordHash = hash,
                    Salt = salt,
                    Rol = item.Rol,
                    Cargo = item.Cargo,
                    Activo = true,
                    IntentosFallidos = 0,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
