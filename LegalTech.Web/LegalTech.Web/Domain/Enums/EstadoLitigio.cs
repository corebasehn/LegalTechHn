namespace LegalTech.Web.Domain.Enums;

/// <summary>
/// Estados procesales de las controversias y litigios marcarios ante la DIGEPIH y tribunales.
/// </summary>
public enum EstadoLitigio
{
    BorradorEstrategia = 1,
    PendienteAutorizacionCliente = 2,
    PresentadoAnteDIGEPIH = 3,
    PeriodoProbatorio = 4,
    AlegatosFinales = 5,
    ResueltoFavorable = 6,
    ResueltoDesfavorable = 7,
    RecurridoReposicion = 8,            // 10 días hábiles
    RecurridoApelacion = 9,             // 3 días hábiles ante Superintendencia de Recursos
    EnContenciosoAdministrativo = 10     // 30 días hábiles ante juzgados
}
