using System;
using System.Collections.Generic;
using LegalTech.Web.Domain.Enums;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Titular de la marca (Persona Natural, Jurídica Nacional o Jurídica Extranjera).
/// </summary>
public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TipoPersona TipoPersona { get; set; } = TipoPersona.NaturalNacional;

    public string NombreRazonSocial { get; set; } = string.Empty;

    public string NumeroIdentificacionRTN { get; set; } = string.Empty; // DNI o RTN

    public string Nacionalidad { get; set; } = "Hondureña";

    public string? EstadoCivil { get; set; } // Obligatorio para personas naturales en Honduras

    public string DireccionExacta { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    // Requisitos especiales para personas jurídicas extranjeras
    public bool TienePoderApostillado { get; set; }
    public bool TieneTraduccionJurada { get; set; }
    public string? CertificadoOrigenUri { get; set; }

    // Relaciones
    public ICollection<Expediente> Expedientes { get; set; } = new List<Expediente>();

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoEn { get; set; }
}
