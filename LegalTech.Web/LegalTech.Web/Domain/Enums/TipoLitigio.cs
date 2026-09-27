namespace LegalTech.Web.Domain.Enums;

/// <summary>
/// Tipos de controversias y acciones legales marcarias reconocidas en el manual.
/// </summary>
public enum TipoLitigio
{
    ContestacionObjecionFondo = 1,      // Defendiendo la solicitud ante reparos de Arts. 83 y 84
    OposicionDefensa = 2,               // Defendiendo la marca del cliente frente a terceros
    OposicionAtaque = 3,                // Bufete atacando solicitud lesiva de un tercero
    AccionCancelacionPorNoUso = 4,      // Causal de 3 años continuos sin uso ni pago de rehabilitación
    AccionNulidad = 5                   // Registro otorgado en contravención a la LPI o derechos previos
}
