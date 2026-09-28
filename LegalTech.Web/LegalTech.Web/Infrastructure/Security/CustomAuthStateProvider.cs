using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using LegalTech.Web.Domain.Entities;

namespace LegalTech.Web.Infrastructure.Security;

/// <summary>
/// Proveedor de estado de autenticación personalizado para Blazor Server.
/// Almacena la sesión de forma encriptada en el navegador del usuario y notifica reactivamente
/// los cambios de estado (Login / Logout / Expiración) a toda la aplicación sin recargas de página.
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private const string SessionStorageKey = "legaltech_secure_session";

    private ClaimsPrincipal _usuarioActualAnonimo = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedLocalStorage localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var storageResult = await _localStorage.GetAsync<UsuarioSesionDto>(SessionStorageKey);

            if (storageResult.Success && storageResult.Value != null)
            {
                var sesion = storageResult.Value;

                // Verificar si la sesión no ha expirado
                if (sesion.FechaExpiracion > DateTime.UtcNow)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, sesion.Id.ToString()),
                        new Claim(ClaimTypes.Name, sesion.NombreCompleto),
                        new Claim(ClaimTypes.Email, sesion.Email),
                        new Claim(ClaimTypes.Role, sesion.Rol),
                        new Claim(ClaimTypes.GivenName, sesion.Username),
                        new Claim("Cargo", sesion.Cargo),
                        new Claim("TokenSesion", sesion.TokenSesion)
                    };

                    var identity = new ClaimsIdentity(claims, "LegalTechAuth");
                    var principal = new ClaimsPrincipal(identity);

                    return new AuthenticationState(principal);
                }
                else
                {
                    // Sesión expirada, limpiar almacenamiento
                    await _localStorage.DeleteAsync(SessionStorageKey);
                }
            }
        }
        catch
        {
            // Puede ocurrir durante prerenderizado o si JavaScript aún no está disponible
        }

        return new AuthenticationState(_usuarioActualAnonimo);
    }

    /// <summary>
    /// Registra la sesión en almacenamiento seguro y notifica a los componentes Blazor.
    /// </summary>
    public async Task IniciarSesionAsync(UsuarioSesionDto sesion)
    {
        await _localStorage.SetAsync(SessionStorageKey, sesion);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, sesion.Id.ToString()),
            new Claim(ClaimTypes.Name, sesion.NombreCompleto),
            new Claim(ClaimTypes.Email, sesion.Email),
            new Claim(ClaimTypes.Role, sesion.Rol),
            new Claim(ClaimTypes.GivenName, sesion.Username),
            new Claim("Cargo", sesion.Cargo),
            new Claim("TokenSesion", sesion.TokenSesion)
        };

        var identity = new ClaimsIdentity(claims, "LegalTechAuth");
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    /// <summary>
    /// Cierra la sesión activa del usuario y notifica a toda la interfaz.
    /// </summary>
    public async Task CerrarSesionAsync()
    {
        try
        {
            await _localStorage.DeleteAsync(SessionStorageKey);
        }
        catch
        {
            // Ignore if already deleted
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_usuarioActualAnonimo)));
    }

    /// <summary>
    /// Retorna los datos de sesión si existe un usuario logueado en este circuito.
    /// </summary>
    public async Task<UsuarioSesionDto?> ObtenerSesionActualAsync()
    {
        try
        {
            var result = await _localStorage.GetAsync<UsuarioSesionDto>(SessionStorageKey);
            if (result.Success && result.Value != null && result.Value.FechaExpiracion > DateTime.UtcNow)
            {
                return result.Value;
            }
        }
        catch
        {
        }
        return null;
    }
}
