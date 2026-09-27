using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio de administración y mantenimiento de Clientes / Titulares de marcas.
/// Garantiza persistencia en base de datos SQLite (cero listas en duro o mocks).
/// </summary>
public class ClienteService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    public ClienteService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Obtiene todos los clientes registrados ordenados alfabéticamente.
    /// </summary>
    public async Task<List<Cliente>> GetClientesAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Clientes
            .Include(c => c.Expedientes)
            .OrderBy(c => c.NombreRazonSocial)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un cliente por su identificador único con sus expedientes asociados.
    /// </summary>
    public async Task<Cliente?> GetClienteByIdAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Clientes
            .Include(c => c.Expedientes)
                .ThenInclude(e => e.EstadoProcesal)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Guarda un cliente nuevo o actualiza uno existente.
    /// </summary>
    public async Task<Cliente> GuardarClienteAsync(Cliente cliente)
    {
        using var db = await _factory.CreateDbContextAsync();

        if (cliente.Id == Guid.Empty)
        {
            cliente.Id = Guid.NewGuid();
            cliente.CreadoEn = DateTime.UtcNow;
            db.Clientes.Add(cliente);
        }
        else
        {
            var existente = await db.Clientes.FindAsync(cliente.Id);
            if (existente == null)
            {
                cliente.CreadoEn = DateTime.UtcNow;
                db.Clientes.Add(cliente);
            }
            else
            {
                existente.NombreRazonSocial = cliente.NombreRazonSocial.Trim();
                existente.NumeroIdentificacionRTN = cliente.NumeroIdentificacionRTN.Trim();
                existente.TipoPersona = cliente.TipoPersona;
                existente.Nacionalidad = cliente.Nacionalidad;
                existente.EstadoCivil = cliente.EstadoCivil;
                existente.DireccionExacta = cliente.DireccionExacta;
                existente.CorreoElectronico = cliente.CorreoElectronico;
                existente.Telefono = cliente.Telefono;
                existente.TienePoderApostillado = cliente.TienePoderApostillado;
                existente.TieneTraduccionJurada = cliente.TieneTraduccionJurada;
                existente.CertificadoOrigenUri = cliente.CertificadoOrigenUri;
                existente.ActualizadoEn = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return cliente;
    }

    /// <summary>
    /// Elimina un cliente si no tiene expedientes activos asociados.
    /// </summary>
    public async Task<(bool Exito, string Mensaje)> EliminarClienteAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var cliente = await db.Clientes
            .Include(c => c.Expedientes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente == null)
        {
            return (false, "El cliente no existe.");
        }

        if (cliente.Expedientes.Any())
        {
            return (false, $"No se puede eliminar el titular '{cliente.NombreRazonSocial}' porque tiene {cliente.Expedientes.Count} expediente(s) marcario(s) registrado(s).");
        }

        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
        return (true, "Cliente eliminado correctamente.");
    }
}
