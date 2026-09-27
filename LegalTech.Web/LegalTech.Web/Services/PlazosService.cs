using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Modelo de evaluación visual del semáforo procesal para plazos fatales.
/// </summary>
public record SemaforoPlazo(
    int DiasHabilesRestantes,
    string ColorBadge,    // "success", "warning", "danger", "dark"
    string TextoBadge,    // Ej: "🟢 22 días hábiles restantes"
    string Icono,         // "check_circle", "schedule", "report_problem", "error"
    string NivelRiesgo,   // "Normal", "Alerta", "Critico", "Vencido"
    string MensajeAccion  // Recomendación procesal para el abogado/paralegal
);

/// <summary>
/// Motor de cálculo de términos y plazos procesales en días hábiles conforme al
/// derecho administrativo hondureño (Art. 42 LPA y Art. 88 Ley de Propiedad Industrial).
/// Excluye automáticamente sábados, domingos, feriados nacionales y asuetos gubernamentales.
/// </summary>
public class PlazosService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    public PlazosService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // 1. MOTOR DE CÓMPUTO DE DÍAS HÁBILES Y FERIADOS
    // =========================================================================

    /// <summary>
    /// Verifica si una fecha determinada es día inhábil (fin de semana o feriado oficial).
    /// </summary>
    public async Task<bool> EsDiaInhabilAsync(DateTime fecha)
    {
        using var db = await _factory.CreateDbContextAsync();
        var feriados = await db.FeriadosNacionales.Where(f => f.Activo).ToListAsync();
        return EsInhabilInternal(fecha, feriados);
    }

    private static bool EsInhabilInternal(DateTime fecha, List<FeriadoNacional> feriados)
    {
        // 1. Sábados y Domingos no son hábiles en sede administrativa
        if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
        {
            return true;
        }

        // 2. Feriados Oficiales y Asuetos de Honduras
        foreach (var f in feriados)
        {
            if (f.EsFijoAnual)
            {
                if (f.Fecha.Month == fecha.Month && f.Fecha.Day == fecha.Day)
                    return true;
            }
            else
            {
                if (f.Fecha.Date == fecha.Date)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calcula la fecha fatal de vencimiento legal en días hábiles.
    /// Conforme al Art. 42 LPA, el cómputo inicia el día hábil SIGUIENTE a la notificación.
    /// </summary>
    public async Task<DateTime> CalcularVencimientoFatalAsync(DateTime fechaNotificacion, int diasHabiles)
    {
        using var db = await _factory.CreateDbContextAsync();
        var feriados = await db.FeriadosNacionales.Where(f => f.Activo).ToListAsync();

        DateTime cursor = fechaNotificacion.Date.AddDays(1);
        int diasContados = 0;

        while (diasContados < diasHabiles)
        {
            if (!EsInhabilInternal(cursor, feriados))
            {
                diasContados++;
                if (diasContados == diasHabiles)
                {
                    return cursor;
                }
            }
            cursor = cursor.AddDays(1);
        }

        return cursor;
    }

    /// <summary>
    /// Calcula los días hábiles que faltan desde hoy hasta la fecha fatal.
    /// Si la fecha ya pasó, devuelve un número negativo que indica los días de atraso.
    /// </summary>
    public async Task<int> CalcularDiasHabilesRestantesAsync(DateTime fechaVencimientoFatal)
    {
        using var db = await _factory.CreateDbContextAsync();
        var feriados = await db.FeriadosNacionales.Where(f => f.Activo).ToListAsync();

        DateTime hoy = DateTime.Today;
        DateTime fatal = fechaVencimientoFatal.Date;

        if (hoy > fatal)
        {
            // Plazo vencido: calcular días hábiles transcurridos desde el vencimiento
            int diasAtraso = 0;
            DateTime cursor = fatal.AddDays(1);
            while (cursor <= hoy)
            {
                if (!EsInhabilInternal(cursor, feriados))
                {
                    diasAtraso++;
                }
                cursor = cursor.AddDays(1);
            }
            return -diasAtraso;
        }
        else
        {
            // Plazo en curso: calcular días hábiles restantes desde hoy hasta el vencimiento
            int diasRestantes = 0;
            DateTime cursor = hoy;
            while (cursor <= fatal)
            {
                if (!EsInhabilInternal(cursor, feriados))
                {
                    diasRestantes++;
                }
                cursor = cursor.AddDays(1);
            }
            return diasRestantes;
        }
    }

    /// <summary>
    /// Evalúa el estado del semáforo procesal según los días hábiles restantes.
    /// </summary>
    public SemaforoPlazo EvaluarSemaforo(int diasHabilesRestantes, bool cumplido)
    {
        if (cumplido)
        {
            return new SemaforoPlazo(
                diasHabilesRestantes,
                "secondary",
                "Evacuado / Cumplido",
                "task_alt",
                "Cumplido",
                "Trámite evacuado oportunamente dentro del término legal."
            );
        }

        if (diasHabilesRestantes < 0)
        {
            return new SemaforoPlazo(
                diasHabilesRestantes,
                "dark",
                $"⚫ Vencido ({Math.Abs(diasHabilesRestantes)} d.h. en mora)",
                "error",
                "Vencido",
                "¡ALERTA CRÍTICA! Término legal fatal expirado. Alto riesgo de resolución de abandono o caducidad."
            );
        }

        if (diasHabilesRestantes == 0)
        {
            return new SemaforoPlazo(
                0,
                "danger",
                "🔴 ¡Vence Hoy!",
                "priority_high",
                "Critico",
                "¡ACCIÓN INMEDIATA! El plazo fatal vence el día de hoy antes del cierre de ventanilla de DIGEPIH."
            );
        }

        if (diasHabilesRestantes <= 5)
        {
            return new SemaforoPlazo(
                diasHabilesRestantes,
                "danger",
                $"🔴 {diasHabilesRestantes} d.h. restantes (Crítico)",
                "report_problem",
                "Critico",
                "Plazo en fase crítica. Radicar escrito de contestación/subsanación a la brevedad."
            );
        }

        if (diasHabilesRestantes <= 15)
        {
            return new SemaforoPlazo(
                diasHabilesRestantes,
                "warning",
                $"🟡 {diasHabilesRestantes} d.h. restantes (Alerta)",
                "schedule",
                "Alerta",
                "Término en curso. Preparar borrador de respuesta y recabar firmas y documentos."
            );
        }

        return new SemaforoPlazo(
            diasHabilesRestantes,
            "success",
            $"🟢 {diasHabilesRestantes} d.h. restantes (En Plazo)",
            "check_circle",
            "Normal",
            "Plazo holgado dentro del calendario procesal administrativo."
        );
    }

    // =========================================================================
    // 2. GESTIÓN DE PLAZOS VINCULADOS AL EXPEDIENTE
    // =========================================================================

    public async Task<List<PlazoLegal>> ObtenerPlazosExpedienteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.PlazosLegales
            .Where(p => p.ExpedienteId == expedienteId)
            .OrderBy(p => p.Cumplido)
            .ThenBy(p => p.FechaVencimientoFatal)
            .ToListAsync();
    }

    /// <summary>
    /// Registra un nuevo plazo procesal calculando automáticamente su fecha fatal en días hábiles.
    /// </summary>
    public async Task<PlazoLegal> RegistrarPlazoAsync(Guid expedienteId, string concepto, int diasHabiles, DateTime fechaNotificacion, string? observaciones = null, string? usuario = null)
    {
        using var db = await _factory.CreateDbContextAsync();
        var expediente = await db.Expedientes.FindAsync(expedienteId) 
            ?? throw new InvalidOperationException("Expediente no encontrado.");

        DateTime fechaFatal = await CalcularVencimientoFatalAsync(fechaNotificacion, diasHabiles);

        var plazo = new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = expedienteId,
            Concepto = concepto,
            DiasHabiles = diasHabiles,
            FechaInicio = fechaNotificacion.Date,
            FechaVencimientoFatal = fechaFatal,
            Cumplido = false
        };

        db.PlazosLegales.Add(plazo);

        // Registrar hito en la línea de tiempo del expediente
        var actuacion = new ActuacionExpediente
        {
            Id = Guid.NewGuid(),
            ExpedienteId = expedienteId,
            TipoActuacionId = concepto.Contains("Prevención", StringComparison.OrdinalIgnoreCase) ? 4 :
                              concepto.Contains("Objeción", StringComparison.OrdinalIgnoreCase) ? 6 :
                              concepto.Contains("Oposición", StringComparison.OrdinalIgnoreCase) ? 11 : 15,
            Titulo = $"Apertura de Plazo Legal: {concepto}",
            Comentario = $"Notificado: {fechaNotificacion:dd/MM/yyyy}. Término fatal: {diasHabiles} días hábiles (Vence: {fechaFatal:dd/MM/yyyy}). {observaciones}".Trim(),
            FechaActuacion = DateTime.UtcNow,
            UsuarioResponsable = usuario ?? "Sistema LegalTech"
        };
        db.ActuacionesExpediente.Add(actuacion);

        await db.SaveChangesAsync();
        return plazo;
    }

    /// <summary>
    /// Marca un plazo procesal como cumplido/evacuado y añade constancia a la línea de tiempo.
    /// </summary>
    public async Task MarcarPlazoCumplidoAsync(Guid plazoId, DateTime fechaCumplimiento, string? notaCumplimiento = null, string? usuario = null)
    {
        using var db = await _factory.CreateDbContextAsync();
        var plazo = await db.PlazosLegales
            .Include(p => p.Expediente)
            .FirstOrDefaultAsync(p => p.Id == plazoId)
            ?? throw new InvalidOperationException("Plazo no encontrado.");

        plazo.Cumplido = true;
        plazo.FechaCumplimiento = fechaCumplimiento;

        var actuacion = new ActuacionExpediente
        {
            Id = Guid.NewGuid(),
            ExpedienteId = plazo.ExpedienteId,
            TipoActuacionId = plazo.Concepto.Contains("Prevención", StringComparison.OrdinalIgnoreCase) ? 5 :
                              plazo.Concepto.Contains("Objeción", StringComparison.OrdinalIgnoreCase) ? 7 : 5,
            Titulo = $"Término Evacuado: {plazo.Concepto}",
            Comentario = $"Evacuado satisfactoriamente el {fechaCumplimiento:dd/MM/yyyy}. {notaCumplimiento}".Trim(),
            FechaActuacion = DateTime.UtcNow,
            UsuarioResponsable = usuario ?? "Abg. Responsable"
        };
        db.ActuacionesExpediente.Add(actuacion);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Elimina un plazo procesal registrado.
    /// </summary>
    public async Task EliminarPlazoAsync(Guid plazoId)
    {
        using var db = await _factory.CreateDbContextAsync();
        var plazo = await db.PlazosLegales.FindAsync(plazoId);
        if (plazo != null)
        {
            db.PlazosLegales.Remove(plazo);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obtiene el plazo activo más urgente de un expediente (para mostrar alertas o insignias en la grilla).
    /// </summary>
    public async Task<(PlazoLegal? Plazo, SemaforoPlazo? Semaforo)> ObtenerPlazoMasUrgenteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        var plazosPendientes = await db.PlazosLegales
            .Where(p => p.ExpedienteId == expedienteId && !p.Cumplido)
            .OrderBy(p => p.FechaVencimientoFatal)
            .ToListAsync();

        if (!plazosPendientes.Any())
        {
            return (null, null);
        }

        var masUrgente = plazosPendientes.First();
        int diasRestantes = await CalcularDiasHabilesRestantesAsync(masUrgente.FechaVencimientoFatal);
        var semaforo = EvaluarSemaforo(diasRestantes, false);

        return (masUrgente, semaforo);
    }

    // =========================================================================
    // 3. ADMINISTRACIÓN DEL CATÁLOGO DE FERIADOS NACIONALES
    // =========================================================================

    public async Task<List<FeriadoNacional>> ObtenerFeriadosAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.FeriadosNacionales
            .OrderBy(f => f.Fecha)
            .ToListAsync();
    }

    public async Task<FeriadoNacional> GuardarFeriadoAsync(FeriadoNacional feriado)
    {
        using var db = await _factory.CreateDbContextAsync();
        if (feriado.Id == 0)
        {
            db.FeriadosNacionales.Add(feriado);
        }
        else
        {
            var existente = await db.FeriadosNacionales.FindAsync(feriado.Id)
                ?? throw new InvalidOperationException("Feriado no encontrado.");

            existente.Nombre = feriado.Nombre;
            existente.Fecha = feriado.Fecha.Date;
            existente.FundamentoLegal = feriado.FundamentoLegal;
            existente.EsFijoAnual = feriado.EsFijoAnual;
            existente.Activo = feriado.Activo;
        }

        await db.SaveChangesAsync();
        return feriado;
    }

    public async Task ToggleFeriadoAsync(int id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var feriado = await db.FeriadosNacionales.FindAsync(id);
        if (feriado != null)
        {
            feriado.Activo = !feriado.Activo;
            await db.SaveChangesAsync();
        }
    }

    public async Task EliminarFeriadoAsync(int id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var feriado = await db.FeriadosNacionales.FindAsync(id);
        if (feriado != null)
        {
            db.FeriadosNacionales.Remove(feriado);
            await db.SaveChangesAsync();
        }
    }
}
