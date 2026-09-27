using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Domain.Enums;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio integral para la gestión de Litigios, Oposiciones, Cancelaciones por No Uso,
/// Nulidades y Recursos Administrativos ante DIGEPIH / IP (Honduras).
/// </summary>
public class LitigioService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;
    private readonly PlazosService _plazosService;

    public LitigioService(IDbContextFactory<LegalTechDbContext> factory, PlazosService plazosService)
    {
        _factory = factory;
        _plazosService = plazosService;
    }

    /// <summary>
    /// Obtiene todos los litigios registrados con sus expedientes y clientes asociados.
    /// Si la tabla está vacía, inicializa datos demostrativos representativos del manual.
    /// </summary>
    public async Task<List<Litigio>> GetLitigiosAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        if (await db.Litigios.CountAsync() < 4)
        {
            await InicializarDatosDemostrativosLitigiosAsync(db);
        }

        return await db.Litigios
            .Include(l => l.Expediente)
                .ThenInclude(e => e!.Cliente)
            .Include(l => l.Expediente)
                .ThenInclude(e => e!.TipoSigno)
            .OrderBy(l => l.FechaVencimientoFatal)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene los litigios vinculados a un expediente específico.
    /// </summary>
    public async Task<List<Litigio>> GetLitigiosPorExpedienteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Litigios
            .Where(l => l.ExpedienteId == expedienteId)
            .Include(l => l.Expediente)
                .ThenInclude(e => e!.Cliente)
            .OrderBy(l => l.FechaVencimientoFatal)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un litigio por su identificador único.
    /// </summary>
    public async Task<Litigio?> GetLitigioByIdAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Litigios
            .Include(l => l.Expediente)
                .ThenInclude(e => e!.Cliente)
            .Include(l => l.Expediente)
                .ThenInclude(e => e!.TipoSigno)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <summary>
    /// Guarda un litigio nuevo o actualiza uno existente.
    /// Si cuenta con expediente vinculado, puede registrar automáticamente el plazo procesal correspondiente.
    /// </summary>
    public async Task<Litigio> GuardarLitigioAsync(Litigio litigio, bool sincronizarPlazoConExpediente = true)
    {
        using var db = await _factory.CreateDbContextAsync();

        if (litigio.Id == Guid.Empty)
        {
            litigio.Id = Guid.NewGuid();
            litigio.CreadoEn = DateTime.UtcNow;
            db.Litigios.Add(litigio);
        }
        else
        {
            var existente = await db.Litigios.FindAsync(litigio.Id);
            if (existente == null)
            {
                litigio.CreadoEn = DateTime.UtcNow;
                db.Litigios.Add(litigio);
            }
            else
            {
                existente.ExpedienteId = litigio.ExpedienteId;
                existente.TipoLitigio = litigio.TipoLitigio;
                existente.ParteDemandante = litigio.ParteDemandante;
                existente.ParteDemandada = litigio.ParteDemandada;
                existente.MarcaEnConflicto = litigio.MarcaEnConflicto;
                existente.FechaNotificacion = litigio.FechaNotificacion;
                existente.FechaVencimientoFatal = litigio.FechaVencimientoFatal;
                existente.AutorizadoPorCliente = litigio.AutorizadoPorCliente;
                existente.AbogadoLitigante = litigio.AbogadoLitigante;
                existente.Estado = litigio.Estado;
                existente.ObservacionesEstrategia = litigio.ObservacionesEstrategia;
                existente.ResultadoFinal = litigio.ResultadoFinal;
                existente.ConstanciaRehabilitacionObtenida = litigio.ConstanciaRehabilitacionObtenida;
                existente.AuditoriaRedesSocialesRealizada = litigio.AuditoriaRedesSocialesRealizada;
                existente.ReporteEvidenciaUsoUri = litigio.ReporteEvidenciaUsoUri;
            }
        }

        await db.SaveChangesAsync();

        // Sincronización automática de término legal con la tabla PlazosLegales del Expediente
        if (sincronizarPlazoConExpediente && litigio.ExpedienteId.HasValue && litigio.ExpedienteId.Value != Guid.Empty)
        {
            await SincronizarPlazoLegalAsync(db, litigio);
        }

        return litigio;
    }

    /// <summary>
    /// Elimina un litigio registrado.
    /// </summary>
    public async Task EliminarLitigioAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var litigio = await db.Litigios.FindAsync(id);
        if (litigio != null)
        {
            db.Litigios.Remove(litigio);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Actualiza el estado procesal y agrega comentarios a la estrategia jurídica.
    /// </summary>
    public async Task CambiarEstadoLitigioAsync(Guid id, EstadoLitigio nuevoEstado, string? notaAdicional = null)
    {
        using var db = await _factory.CreateDbContextAsync();
        var litigio = await db.Litigios.FindAsync(id);
        if (litigio != null)
        {
            litigio.Estado = nuevoEstado;
            if (!string.IsNullOrWhiteSpace(notaAdicional))
            {
                string sello = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm}] {notaAdicional}";
                litigio.ObservacionesEstrategia = string.IsNullOrWhiteSpace(litigio.ObservacionesEstrategia)
                    ? sello
                    : $"{litigio.ObservacionesEstrategia}\n{sello}";
            }

            // Si pasa a presentado y tiene expediente, registrar la actuación en el expediente
            if (nuevoEstado == EstadoLitigio.PresentadoAnteDIGEPIH && litigio.ExpedienteId.HasValue)
            {
                var actuacion = new ActuacionExpediente
                {
                    ExpedienteId = litigio.ExpedienteId.Value,
                    TipoActuacionId = 5, // Contestación
                    Titulo = $"Escrito Radicado: {litigio.TipoLitigio}",
                    Comentario = $"Se radicó escrito formal ante DIGEPIH en litigio contra {litigio.ParteDemandada}. Abogado: {litigio.AbogadoLitigante ?? "Titular"}.",
                    FechaActuacion = DateTime.UtcNow,
                    UsuarioResponsable = litigio.AbogadoLitigante ?? "Abogado Litigante"
                };
                db.ActuacionesExpediente.Add(actuacion);
            }

            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Registra la interposición de un recurso administrativo o judicial con su plazo legal fatal:
    /// - Reposición: 10 días hábiles
    /// - Apelación: 3 días hábiles (Superintendencia de Recursos)
    /// - Contencioso-Administrativo: 30 días hábiles (Juzgados)
    /// </summary>
    public async Task RegistrarRecursoAsync(Guid id, EstadoLitigio tipoRecurso, DateTime fechaNotificacionResolucion, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var litigio = await db.Litigios.FindAsync(id)
            ?? throw new InvalidOperationException("Litigio no encontrado.");

        int diasHabiles = tipoRecurso switch
        {
            EstadoLitigio.RecurridoReposicion => 10,
            EstadoLitigio.RecurridoApelacion => 3,
            EstadoLitigio.EnContenciosoAdministrativo => 30,
            _ => 10
        };

        DateTime vencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(fechaNotificacionResolucion, diasHabiles);

        litigio.Estado = tipoRecurso;
        litigio.FechaNotificacion = fechaNotificacionResolucion;
        litigio.FechaVencimientoFatal = vencimientoFatal;

        string nombreRecurso = tipoRecurso switch
        {
            EstadoLitigio.RecurridoReposicion => "Recurso de Reposición (10 d.h.)",
            EstadoLitigio.RecurridoApelacion => "Recurso de Apelación (3 d.h. ante Superintendencia de Recursos)",
            EstadoLitigio.EnContenciosoAdministrativo => "Demanda Contencioso-Administrativa (30 d.h.)",
            _ => "Recurso Procesal"
        };

        string nota = $"[{DateTime.UtcNow:dd/MM/yyyy}] Interposición de {nombreRecurso}. Notificado: {fechaNotificacionResolucion:dd/MM/yyyy}. Vencimiento Fatal: {vencimientoFatal:dd/MM/yyyy}. Por: {usuario}";
        litigio.ObservacionesEstrategia = string.IsNullOrWhiteSpace(litigio.ObservacionesEstrategia)
            ? nota
            : $"{litigio.ObservacionesEstrategia}\n{nota}";

        await db.SaveChangesAsync();

        if (litigio.ExpedienteId.HasValue)
        {
            await SincronizarPlazoLegalAsync(db, litigio);
        }
    }

    /// <summary>
    /// Sincroniza o crea el PlazoLegal correspondiente en el expediente.
    /// </summary>
    private async Task SincronizarPlazoLegalAsync(LegalTechDbContext db, Litigio litigio)
    {
        if (!litigio.ExpedienteId.HasValue) return;

        string concepto = $"Litigio: {litigio.TipoLitigio}";
        var plazoExistente = await db.PlazosLegales
            .FirstOrDefaultAsync(p => p.ExpedienteId == litigio.ExpedienteId.Value && p.Concepto.Contains(litigio.TipoLitigio.ToString()));

        if (plazoExistente == null)
        {
            var nuevoPlazo = new PlazoLegal
            {
                ExpedienteId = litigio.ExpedienteId.Value,
                Concepto = $"Litigio: {litigio.TipoLitigio} ({litigio.MarcaEnConflicto})",
                DiasHabiles = Math.Max(1, (int)(litigio.FechaVencimientoFatal - litigio.FechaNotificacion).TotalDays),
                FechaInicio = litigio.FechaNotificacion,
                FechaVencimientoFatal = litigio.FechaVencimientoFatal,
                Cumplido = litigio.Estado == EstadoLitigio.ResueltoFavorable || litigio.Estado == EstadoLitigio.ResueltoDesfavorable,
                FechaCumplimiento = (litigio.Estado == EstadoLitigio.ResueltoFavorable || litigio.Estado == EstadoLitigio.ResueltoDesfavorable) ? DateTime.UtcNow : null
            };
            db.PlazosLegales.Add(nuevoPlazo);
        }
        else
        {
            plazoExistente.FechaVencimientoFatal = litigio.FechaVencimientoFatal;
            plazoExistente.Cumplido = litigio.Estado == EstadoLitigio.ResueltoFavorable || litigio.Estado == EstadoLitigio.ResueltoDesfavorable;
            if (plazoExistente.Cumplido && !plazoExistente.FechaCumplimiento.HasValue)
            {
                plazoExistente.FechaCumplimiento = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Puebla la base de datos con 6 controversias y litigios reales para demostrar cada caso de uso:
    /// - Oposición en Defensa (4 d.h. restantes - Crítico)
    /// - Oposición en Ataque (12 d.h. restantes - Alerta)
    /// - Contestación Objeción Fondo (38 d.h. restantes - En Plazo)
    /// - Cancelación por No Uso (con checklist de redes sociales y constancia de rehabilitación)
    /// - Acción de Nulidad (con fundamento en Arts. 83 y 84 LPI)
    /// - Recurso de Apelación (2 d.h. restantes - Vencimiento Inminente)
    /// </summary>
    private async Task InicializarDatosDemostrativosLitigiosAsync(LegalTechDbContext db)
    {
        var expedientes = await db.Expedientes.OrderBy(e => e.CodigoInterno).ToListAsync();
        DateTime hoy = DateTime.Today;

        // Limpiar litigios previos si los hubiera
        db.Litigios.RemoveRange(db.Litigios);
        await db.SaveChangesAsync();

        var demoLitigios = new List<Litigio>
        {
            // 1. OPOSICIÓN EN DEFENSA (🔴 CRÍTICO: 4 días hábiles restantes para contestar)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0004")?.Id ?? expedientes.FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.OposicionDefensa,
                ParteDemandante = "CORPORACIÓN ALIMENTARIA CENTROAMERICANA S.A.",
                ParteDemandada = "INVERSIONES GASTRONÓMICAS DE HONDURAS S.A.",
                MarcaEnConflicto = "BALEADAS EXPRESS vs BALEADAS LA EXPRESA DE COMAYAGUA",
                FechaNotificacion = await _plazosService.CalcularVencimientoFatalAsync(hoy.AddDays(-10), 1),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 4), // 4 d.h. restantes
                AutorizadoPorCliente = false,
                AbogadoLitigante = "Abg. Carlos Mendoza (Litigios)",
                Estado = EstadoLitigio.PendienteAutorizacionCliente,
                ObservacionesEstrategia = "Término de 10 días hábiles (Manual Párr. 297). Opositora alega similitud gráfica en Clase 43. Urge autorización del cliente y firma para radicar contestación antes del cierre de ventanilla de DIGEPIH.",
                ConstanciaRehabilitacionObtenida = false,
                AuditoriaRedesSocialesRealizada = false,
                CreadoEn = DateTime.UtcNow.AddDays(-5)
            },

            // 2. OPOSICIÓN EN ATAQUE (🟡 ALERTA: 11 días hábiles restantes tras 3er aviso en La Gaceta)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0002")?.Id ?? expedientes.Skip(1).FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.OposicionAtaque,
                ParteDemandante = "VALLE TECH SOFTWARE S.A. (Nuestro Cliente)",
                ParteDemandada = "INNOVACIONES DIGITALES DE HONDURAS S. DE R.L.",
                MarcaEnConflicto = "VALLE TECH INNOVATION (Aviso publicado en La Gaceta)",
                FechaNotificacion = hoy.AddDays(-15),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 11), // 11 d.h. restantes
                AutorizadoPorCliente = true,
                AbogadoLitigante = "Abg. Fernando Morales (Litigios)",
                Estado = EstadoLitigio.BorradorEstrategia,
                ObservacionesEstrategia = "Plazo de 30 días hábiles posteriores a la 3ª publicación oficial (Manual Párr. 396). El signo del tercero invade Clase 42 provocando riesgo de dilución y confusión en el mercado tecnológico hondureño. Poder especial convalidado.",
                ConstanciaRehabilitacionObtenida = false,
                AuditoriaRedesSocialesRealizada = false,
                CreadoEn = DateTime.UtcNow.AddDays(-12)
            },

            // 3. CONTESTACIÓN DE OBJECIÓN DE FONDO (🟢 EN PLAZO: 35 días hábiles restantes)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0001")?.Id ?? expedientes.FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.ContestacionObjecionFondo,
                ParteDemandante = "DIGEPIH / Oficina de Marcas de Honduras",
                ParteDemandada = "CAFÉ DE LA SIERRA MONTES S. DE R.L.",
                MarcaEnConflicto = "CAFÉ DE LA SIERRA MONTES (Objeción Art. 83 LPI)",
                FechaNotificacion = hoy.AddDays(-20),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 35), // 35 d.h. restantes
                AutorizadoPorCliente = true,
                AbogadoLitigante = "Abg. Elena Durón (Propiedad Intelectual)",
                Estado = EstadoLitigio.PresentadoAnteDIGEPIH,
                ObservacionesEstrategia = "Objeción formal por vocablo geográfico 'SIERRA'. Memorial radicado sustentando distintividad adquirida (secondary meaning) y limitación específica en Clase 30 a café gourmet de altura.",
                ConstanciaRehabilitacionObtenida = false,
                AuditoriaRedesSocialesRealizada = false,
                CreadoEn = DateTime.UtcNow.AddDays(-20)
            },

            // 4. ACCIÓN DE CANCELACIÓN POR NO USO (3 AÑOS CONTINUOS - Manual Párrs. 416-438)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0005")?.Id ?? expedientes.Skip(2).FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.AccionCancelacionPorNoUso,
                ParteDemandante = "ROATAN HOSPITALITY GROUP S.A. (Nuestro Cliente)",
                ParteDemandada = "CARIBBEAN TOURS & LEISURE CORP.",
                MarcaEnConflicto = "TURQUOISE BAY CARIBBEAN (Registro No. 45,120)",
                FechaNotificacion = hoy.AddDays(-8),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 18),
                AutorizadoPorCliente = true,
                AbogadoLitigante = "Abg. Fernando Morales (Litigios)",
                Estado = EstadoLitigio.PeriodoProbatorio,
                ObservacionesEstrategia = "Acción promovida para limpiar el registro previo en Clase 43. Constancia de no rehabilitación acreditada en DIGEPIH y auditoría digital negativa en redes sociales y buscadores web.",
                ConstanciaRehabilitacionObtenida = true,
                AuditoriaRedesSocialesRealizada = true,
                ReporteEvidenciaUsoUri = "docs/auditoria_redes_turquoise_bay.pdf",
                CreadoEn = DateTime.UtcNow.AddDays(-8)
            },

            // 5. ACCIÓN DE NULIDAD DE REGISTRO (ARTS. 83 Y 84 LPI - Manual Párrs. 439-450)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0006")?.Id ?? expedientes.Skip(3).FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.AccionNulidad,
                ParteDemandante = "COOPERATIVA GANADERA DEL SUR LIMITADA",
                ParteDemandada = "AGROPECUARIA CONTINENTAL S.A.",
                MarcaEnConflicto = "DEL SUR GOURMET (Registro concedido No. 51,880)",
                FechaNotificacion = hoy.AddDays(-14),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 22),
                AutorizadoPorCliente = true,
                AbogadoLitigante = "Abg. Carlos Mendoza (Litigios)",
                Estado = EstadoLitigio.AlegatosFinales,
                ObservacionesEstrategia = "Demanda de nulidad fundamentada en infracción al derecho de prelación del Art. 84 inc. b) LPI. Conclusiones y alegatos finales listos para firma y sentencia de primera instancia.",
                ConstanciaRehabilitacionObtenida = false,
                AuditoriaRedesSocialesRealizada = false,
                CreadoEn = DateTime.UtcNow.AddDays(-14)
            },

            // 6. RECURSO DE APELACIÓN (🔴 VENCIMIENTO INMINENTE: 2 d.h. restantes)
            new()
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0003")?.Id ?? expedientes.Skip(4).FirstOrDefault()?.Id,
                TipoLitigio = TipoLitigio.ContestacionObjecionFondo,
                ParteDemandante = "DIGEPIH / Superintendencia de Recursos",
                ParteDemandada = "INDUSTRIAS TEXTILES DE CHOLOMA S.A.",
                MarcaEnConflicto = "CHOLOMA ACTIVEWEAR (Recurso de Apelación)",
                FechaNotificacion = hoy.AddDays(-1),
                FechaVencimientoFatal = await _plazosService.CalcularVencimientoFatalAsync(hoy, 2), // 2 d.h. restantes
                AutorizadoPorCliente = true,
                AbogadoLitigante = "Abg. Carlos Mendoza (Litigios)",
                Estado = EstadoLitigio.RecurridoApelacion,
                ObservacionesEstrategia = "¡TÉRMINO FATAL DE 3 DÍAS HÁBILES! (Art. 138 LPA y Manual Párr. 388). Apelación ante la Superintendencia de Recursos en contra de la resolución denegatoria de reposición. Escrito en revisión final.",
                ConstanciaRehabilitacionObtenida = false,
                AuditoriaRedesSocialesRealizada = false,
                CreadoEn = DateTime.UtcNow.AddDays(-2)
            }
        };

        db.Litigios.AddRange(demoLitigios);
        await db.SaveChangesAsync();
    }
}
