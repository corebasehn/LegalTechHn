namespace LegalTech.Web.Domain.Enums;

/// <summary>
/// Naturaleza jurídica del titular para validación estricta de requisitos documentales en Honduras.
/// </summary>
public enum TipoPersona
{
    NaturalNacional = 1,        // Requiere DNI y estado civil
    JuridicaNacional = 2,       // Requiere Escritura autenticada y RTN mercantil
    JuridicaExtranjera = 3,     // Requiere Certificado origen, Apostilla de La Haya y Traducción jurada
    NaturalExtranjera = 4       // Persona natural extranjera (Pasaporte, Poder especial apostillado)
}
