using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Infrastructure.Data;

namespace LegalTech.Web.Services;

/// <summary>
/// Servicio integral de gestión, trazabilidad y actuaciones procesales de expedientes marcarios ante la DIGEPIH.
/// </summary>
public class ExpedienteService
{
    private readonly IDbContextFactory<LegalTechDbContext> _contextFactory;

    public ExpedienteService(IDbContextFactory<LegalTechDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Obtiene la lista de expedientes para la tabla principal.
    /// </summary>
    public async Task<List<Expediente>> GetExpedientesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.Expedientes.CountAsync() < 6)
        {
            await InicializarDatosDemostrativosInternoAsync(context);
        }

        return await context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.TipoSigno)
            .Include(e => e.EstadoProcesal)
            .Include(e => e.Publicaciones)
            .Include(e => e.PlazosLegales)
            .OrderBy(e => e.CodigoInterno)
            .ToListAsync();
    }

    private async Task InicializarDatosDemostrativosInternoAsync(LegalTechDbContext context)
    {
        // Limpiar datos demostrativos previos de expedientes (la tabla Clientes se preserva)
        context.PlazosLegales.RemoveRange(context.PlazosLegales);
        context.ActuacionesExpediente.RemoveRange(context.ActuacionesExpediente);
        context.PublicacionesENAG.RemoveRange(context.PublicacionesENAG);
        context.Litigios.RemoveRange(context.Litigios);
        context.Certificados.RemoveRange(context.Certificados);
        context.Expedientes.RemoveRange(context.Expedientes);
        await context.SaveChangesAsync();

        // 1. Obtener los titulares directamente desde la tabla Clientes en la base de datos
        var clientes = await context.Clientes.OrderBy(c => c.NombreRazonSocial).ToListAsync();
        if (!clientes.Any())
        {
            // Si la tabla estuviese vacía por primera ejecución, poblarla en base de datos
            clientes = new List<Cliente>
            {
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "INDUSTRIAS TEXTILES DE CHOLOMA S.A.", NumeroIdentificacionRTN = "05011995887766", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "legal@textilescholoma.hn", Telefono = "+504 2669-4500", DireccionExacta = "ZIP Choloma, Nave 14, Boulevard a Puerto Cortés, Choloma, Cortés, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "ROATAN HOSPITALITY GROUP S.A.", NumeroIdentificacionRTN = "11012010445566", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "info@roatanhospitality.com", Telefono = "+504 2455-7800", DireccionExacta = "West Bay Beach, Calle Principal Edificio Turquoise, Roatán, Islas de la Bahía, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "COOPERATIVA GANADERA DEL SUR LIMITADA", NumeroIdentificacionRTN = "06011988332211", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "coop@ganaderadelsur.hn", Telefono = "+504 2782-1200", DireccionExacta = "Carretera Panamericana Km 5, Salida a San Marcos de Colón, Choluteca, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "INVERSIONES COPAN S. DE R.L.", NumeroIdentificacionRTN = "08011990123456", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "legal@inversionescopan.hn", Telefono = "+504 2239-1122", DireccionExacta = "Col. Palmira, Ave. República de Chile, Edificio Copán 4to Piso, Tegucigalpa, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "CERVECERIA DEL VALLE S.A.", NumeroIdentificacionRTN = "05011985654321", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "propiedad.intelectual@valle.hn", Telefono = "+504 2552-3344", DireccionExacta = "Boulevard del Norte, Parque Industrial El Valle, San Pedro Sula, Cortés, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), NombreRazonSocial = "DISTRIBUIDORA FARMACEUTICA DEL CARIBE S.A.", NumeroIdentificacionRTN = "05011999881122", TipoPersona = Domain.Enums.TipoPersona.JuridicaNacional, CorreoElectronico = "asuntos.regulatorios@farmaciascaribe.hn", Telefono = "+504 2557-9000", DireccionExacta = "Barrio Guamilito, 8 Calle 5 Ave NO, San Pedro Sula, Cortés, Honduras", Nacionalidad = "Hondureña", CreadoEn = DateTime.UtcNow }
            };
            context.Clientes.AddRange(clientes);
            await context.SaveChangesAsync();
        }

        var clienteCholoma = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("TEXTILES")) ?? clientes[0];
        var clienteRoatan = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("ROATAN")) ?? (clientes.Count > 1 ? clientes[1] : clientes[0]);
        var clienteSur = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("GANADERA")) ?? (clientes.Count > 2 ? clientes[2] : clientes[0]);
        var clienteCopan = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("COPAN")) ?? (clientes.Count > 3 ? clientes[3] : clientes[0]);
        var clienteValle = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("VALLE")) ?? (clientes.Count > 4 ? clientes[4] : clientes[0]);
        var clienteFarmacia = clientes.FirstOrDefault(c => c.NombreRazonSocial.Contains("FARMACEUTICA")) ?? (clientes.Count > 5 ? clientes[5] : clientes[0]);

        // =========================================================================
        // EXPEDIENTE 1: ETAPA 1 - APERTURA / BÚSQUEDA PRELIMINAR
        // Marca en dictamen previo, sin presentación formal aún.
        // =========================================================================
        var exp1 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0001",
            ClienteId = clienteCholoma.Id,
            DenominacionMarca = "PUMA TEXTIL HONDURAS",
            TipoSignoId = 5, // Mixta
            ClaseNiza = 25,  // Prendas de vestir
            DescripcionEspecificaProductosServicios = "Prendas de vestir confeccionadas en algodón y fibras sintéticas, ropa deportiva de alto rendimiento, camisetas polo, uniformes corporativos y calcetería de exportación.",
            NumeroExpedienteDIGEPIH = null, // Aún no presentado formalmente
            FechaPresentacionOficial = null,
            ViaPresentacionId = 1,
            EstadoProcesalId = 2, // Búsqueda Preliminar y Viabilidad (Fase: Apertura)
            TimbreL50Pagado = false,
            EtiquetasFisicas2x4Entregadas = false,
            TasasOficialesPagadas = false,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddDays(-12)
        };

        exp1.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1, // Ingreso de Instrucciones
            Titulo = "Apertura de Expediente y Recepción de Instrucciones",
            Comentario = "Cliente remite logotipo y solicita estudio registral previo para prendas de vestir Clase 25.",
            FechaActuacion = DateTime.UtcNow.AddDays(-12),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp1.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 2, // Búsqueda Registral
            Titulo = "Búsqueda Fonética y Gráfica Realizada en DIGEPIH",
            Comentario = "Dictamen de viabilidad favorable emitido al cliente. No se encontraron marcas idénticas registradas en Honduras. Se sugiere proceder con la solicitud formal.",
            FechaActuacion = DateTime.UtcNow.AddDays(-8),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        // =========================================================================
        // EXPEDIENTE 2: ETAPA 2 - PRESENTACIÓN FORMAL ANTE DIGEPIH
        // Solicitud recién radicada en ventanilla, fija fecha de prioridad.
        // =========================================================================
        var exp2 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0002",
            ClienteId = clienteRoatan.Id,
            DenominacionMarca = "MAHOGANY BAY RESORT & SPA",
            TipoSignoId = 2, // Marca de Servicios
            ClaseNiza = 43,  // Hotelería y hospedaje temporal
            DescripcionEspecificaProductosServicios = "Servicios de hotelería de playa, alojamiento vacacional temporal, villas turísticas, restaurantes de especialidades caribeñas y servicio de bar.",
            NumeroExpedienteDIGEPIH = "2026-01254",
            FechaPresentacionOficial = DateTime.UtcNow.AddDays(-9),
            ViaPresentacionId = 1, // Ventanilla Tegucigalpa (Centro Cívico)
            EstadoProcesalId = 5, // Presentado ante DIGEPIH (Fase: Presentación)
            TimbreL50Pagado = true,
            EtiquetasFisicas2x4Entregadas = true,
            TasasOficialesPagadas = true,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddDays(-20)
        };

        exp2.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1,
            Titulo = "Apertura de Expediente Turístico",
            Comentario = "Instrucciones de Roatan Hospitality Group para protección en Clase 43.",
            FechaActuacion = DateTime.UtcNow.AddDays(-20),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp2.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 3, // Presentación Oficial
            Titulo = "Presentación Oficial ante DIGEPIH - No. 2026-01254",
            Comentario = "Ingreso formal de solicitud en ventanilla del Centro Cívico Gubernamental. Fija fecha de prelación legal en Honduras.",
            FechaActuacion = DateTime.UtcNow.AddDays(-9),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp2.PlazosLegales.Add(new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp2.Id,
            Concepto = "Requerimiento de Acreditación de Personería y Poder (Art. 88 LPI)",
            DiasHabiles = 10,
            FechaInicio = DateTime.Today.AddDays(-8),
            FechaVencimientoFatal = DateTime.Today.AddDays(3), // Crítico (3 días hábiles restantes <= 5 d.h.) -> 🔴 Rojo
            Cumplido = false
        });

        // =========================================================================
        // EXPEDIENTE 3: ETAPA 3 - EXAMEN (CON PREVENCIÓN FORMAL DE 30 DÍAS)
        // Auto de prevención notificado por DIGEPIH.
        // =========================================================================
        var exp3 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0003",
            ClienteId = clienteSur.Id,
            DenominacionMarca = "SUPER LACTEOS DEL SUR",
            TipoSignoId = 1, // Marca de Fábrica
            ClaseNiza = 29,  // Productos lácteos
            DescripcionEspecificaProductosServicios = "Leche pasteurizada entera y semidescremada, quesos frescos y ahumados artesanales, mantequilla lavada, crema pura de leche y cuajada.",
            NumeroExpedienteDIGEPIH = "2026-00812",
            FechaPresentacionOficial = DateTime.UtcNow.AddDays(-40),
            ViaPresentacionId = 1,
            EstadoProcesalId = 7, // Con Prevención de Forma (30 días) (Fase: Examen)
            TimbreL50Pagado = true,
            EtiquetasFisicas2x4Entregadas = true,
            TasasOficialesPagadas = true,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddDays(-50)
        };

        exp3.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1,
            Titulo = "Apertura de Expediente",
            Comentario = "Solicitud de la Cooperativa Ganadera del Sur para su línea de lácteos.",
            FechaActuacion = DateTime.UtcNow.AddDays(-50),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp3.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 3,
            Titulo = "Presentación Oficial ante DIGEPIH - No. 2026-00812",
            Comentario = "Solicitud radicada ante ventanilla de propiedad industrial.",
            FechaActuacion = DateTime.UtcNow.AddDays(-40),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp3.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 4, // Auto de Prevención de Forma
            Titulo = "Notificación de Auto de Prevención de Forma (DIGEPIH)",
            Comentario = "El examinador formal previene subsanar la especificación de productos por contener expresiones ambiguas y solicita autentica notarial del poder. Se otorgan 30 días hábiles para evacuar prevención.",
            FechaActuacion = DateTime.UtcNow.AddDays(-10),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp3.PlazosLegales.Add(new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp3.Id,
            Concepto = "Auto de Prevención de Forma (Subsanar Requisitos y Poder)",
            DiasHabiles = 30,
            FechaInicio = DateTime.Today.AddDays(-15),
            FechaVencimientoFatal = DateTime.Today.AddDays(13), // Alerta (~9-10 días hábiles restantes: 6 a 15 d.h.) -> 🟡 Ámbar
            Cumplido = false
        });

        // =========================================================================
        // EXPEDIENTE 4: ETAPA 4 - PUBLICACIÓN EN LA GACETA (ENAG)
        // 2 avisos publicados, cálculo de plazo de oposición y pendiente entrega física.
        // =========================================================================
        var exp4 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0004",
            ClienteId = clienteCopan.Id,
            DenominacionMarca = "CAFÉ DE LA SIERRA MONTES",
            TipoSignoId = 5, // Mixta
            ClaseNiza = 30,  // Café gourmet
            DescripcionEspecificaProductosServicios = "Café en grano tostado, café molido gourmet, café liofilizado soluble e infusiones de café de estricta altura de la Reserva Biológica de Montecillos.",
            NumeroExpedienteDIGEPIH = "2026-00482",
            FechaPresentacionOficial = DateTime.UtcNow.AddDays(-80),
            ViaPresentacionId = 1,
            EstadoProcesalId = 14, // En Publicación La Gaceta (3 avisos) (Fase: Publicación)
            TimbreL50Pagado = true,
            EtiquetasFisicas2x4Entregadas = true,
            TasasOficialesPagadas = true,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddDays(-90)
        };

        exp4.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1,
            Titulo = "Apertura de Expediente Marcarío",
            Comentario = "Instrucciones de Inversiones Copán para registro de marca de café.",
            FechaActuacion = DateTime.UtcNow.AddDays(-90),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp4.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 3,
            Titulo = "Presentación Oficial ante DIGEPIH - No. 2026-00482",
            Comentario = "Ingreso formal y radicación en ventanilla de DIGEPIH.",
            FechaActuacion = DateTime.UtcNow.AddDays(-80),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp4.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 8, // Emisión Aviso Publicación
            Titulo = "Emisión de Aviso de Publicación por DIGEPIH",
            Comentario = "Examen de forma y fondo concluidos satisfactoriamente. Se autoriza la publicación en el Diario Oficial La Gaceta.",
            FechaActuacion = DateTime.UtcNow.AddDays(-30),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp4.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 9, // Aviso Publicado
            Titulo = "Publicación del 1er Aviso en La Gaceta No. 36,412",
            Comentario = "Primer aviso publicado. Inicia cómputo de 15 días para la segunda publicación.",
            FechaActuacion = DateTime.UtcNow.AddDays(-22),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp4.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 9, // Aviso Publicado
            Titulo = "Publicación del 2do Aviso en La Gaceta No. 36,427",
            Comentario = "Segundo aviso publicado. Pendiente 3er aviso y adquisición de ejemplares físicos para evitar abandono.",
            FechaActuacion = DateTime.UtcNow.AddDays(-6),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        var pub4 = new PublicacionENAG
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp4.Id,
            NumeroAvisoDIGEPIH = "AV-2026-0391",
            ReciboPagoENAG = "ENAG-REC-88492",
            FechaPublicacionAviso1 = DateTime.UtcNow.AddDays(-22),
            FechaPublicacionAviso2 = DateTime.UtcNow.AddDays(-6),
            FechaPublicacionAviso3 = null,
            EjemplaresFisicosRecibidos = false,
            EjemplaresEntregadosDIGEPIH = false
        };
        exp4.Publicaciones.Add(pub4);

        exp4.PlazosLegales.Add(new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp4.Id,
            Concepto = "Período de Oposición a Terceros tras Publicaciones (30 d.h.)",
            DiasHabiles = 30,
            FechaInicio = DateTime.Today.AddDays(-2),
            FechaVencimientoFatal = DateTime.Today.AddDays(38), // En curso (~27 días hábiles restantes > 15 d.h.) -> 🟢 Verde
            Cumplido = false
        });

        // =========================================================================
        // EXPEDIENTE 5: ETAPA 5 - CONCESIÓN Y TÍTULO (BLOQUEO SOLVENCIA P360)
        // Marca concedida, título en Bóveda, bloqueado por regla P360.
        // =========================================================================
        var exp5 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0005",
            ClienteId = clienteValle.Id,
            DenominacionMarca = "VALLE TECH SOFTWARE",
            TipoSignoId = 2, // Marca de Servicios
            ClaseNiza = 42,  // Servicios tecnológicos y software
            DescripcionEspecificaProductosServicios = "Diseño, desarrollo, mantenimiento y consultoría de arquitecturas de software empresarial y soluciones informáticas en la nube.",
            NumeroExpedienteDIGEPIH = "2025-01890",
            FechaPresentacionOficial = DateTime.UtcNow.AddMonths(-11),
            ViaPresentacionId = 1,
            EstadoProcesalId = 19, // Certificado de Registro Emitido (Fase: Concesión)
            TimbreL50Pagado = true,
            EtiquetasFisicas2x4Entregadas = true,
            TasasOficialesPagadas = true,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddMonths(-11)
        };

        exp5.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1,
            Titulo = "Apertura de Expediente de Software",
            Comentario = "Instrucciones de Cervecería del Valle S.A. para registro en Clase 42.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-11),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp5.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 3,
            Titulo = "Presentación Oficial ante DIGEPIH - No. 2025-01890",
            Comentario = "Radicación formal de la solicitud marcaria.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-11),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp5.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 12, // Resolución Concesión
            Titulo = "Resolución Favorable de Concesión Marcario Definitiva",
            Comentario = "DIGEPIH emite resolución concediendo el registro de marca tras superar el término de oposición sin controversias.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-2),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp5.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 13, // Certificado Custodiado
            Titulo = "Certificado Oficial de Registro No. 2026-REG-0112 Emitido",
            Comentario = "Título oficial recepcionado y resguardado en Bóveda de Títulos. Vigencia decenal garantizada. Pendiente validación de solvencia P360 para entrega.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-1),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        var cert5 = new CertificadoRegistro
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp5.Id,
            NumeroRegistroOficial = "2026-REG-0112",
            FechaConcesion = DateTime.UtcNow.AddMonths(-1),
            FechaVencimientoDecenal = DateTime.UtcNow.AddMonths(-1).AddYears(10),
            UbicacionArchivoId = 3, // Bóveda de Seguridad
            SolvenciaValidada = false // BLOQUEO P360 ACTIVO
        };
        exp5.Certificado = cert5;

        exp5.PlazosLegales.Add(new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp5.Id,
            Concepto = "Pago de Tasa Oficial de Concesión y Registro",
            DiasHabiles = 30,
            FechaInicio = DateTime.Today.AddMonths(-3),
            FechaVencimientoFatal = DateTime.Today.AddMonths(-2),
            FechaCumplimiento = DateTime.Today.AddMonths(-2).AddDays(-5),
            Cumplido = true // Evacuado -> ⚪ Al Día
        });

        // =========================================================================
        // EXPEDIENTE 6: ETAPA 3 - OBJECIÓN DE FONDO VENCIDA (RIESGO DE ABANDONO)
        // Término de 60 días hábiles vencido en mora procesal.
        // =========================================================================
        var exp6 = new Expediente
        {
            Id = Guid.NewGuid(),
            CodigoInterno = "EXP-2026-0006",
            ClienteId = clienteFarmacia.Id,
            DenominacionMarca = "BIOMEDIC LABS",
            TipoSignoId = 5, // Mixta
            ClaseNiza = 5,   // Productos farmacéuticos y preparaciones medicinales
            DescripcionEspecificaProductosServicios = "Preparaciones farmacéuticas para uso humano, suplementos vitamínicos, medicamentos antivirales y soluciones desinfectantes de grado clínico hospitalario.",
            NumeroExpedienteDIGEPIH = "2025-02941",
            FechaPresentacionOficial = DateTime.UtcNow.AddMonths(-6),
            ViaPresentacionId = 1,
            EstadoProcesalId = 10, // Con Objeción de Fondo (60 días) (Fase: Examen)
            TimbreL50Pagado = true,
            EtiquetasFisicas2x4Entregadas = true,
            TasasOficialesPagadas = true,
            AbogadoResponsable = "Abg. Marco Tulio Valle",
            ParalegalAsignado = "Lic. Andrea Paz",
            CreadoEn = DateTime.UtcNow.AddMonths(-6)
        };

        exp6.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1,
            Titulo = "Apertura de Expediente Farmacéutico",
            Comentario = "Instrucciones de Distribuidora Farmacéutica del Caribe S.A. para registro en Clase 5.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-6),
            UsuarioResponsable = "Lic. Andrea Paz"
        });

        exp6.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 3,
            Titulo = "Presentación Oficial ante DIGEPIH - No. 2025-02941",
            Comentario = "Ingreso formal de la solicitud marcaria en ventanilla.",
            FechaActuacion = DateTime.UtcNow.AddMonths(-6),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp6.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 6, // Auto de Objeción de Fondo
            Titulo = "Notificación de Auto de Objeción de Fondo (DIGEPIH)",
            Comentario = "El examinador objeta de oficio la solicitud por supuesta semejanza fonética con la marca 'BIOMEDICA'. Se otorgaron 60 días hábiles para contestar.",
            FechaActuacion = DateTime.UtcNow.AddDays(-95),
            UsuarioResponsable = "Abg. Marco Tulio Valle"
        });

        exp6.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 15, // Otra Actuación
            Titulo = "ALERTA: Término Perentorio Fatal Vencido",
            Comentario = "El término de 60 días hábiles para contestar la objeción ha expirado sin evacuación. Alto riesgo de abandono y archivo definitivo.",
            FechaActuacion = DateTime.UtcNow.AddDays(-4),
            UsuarioResponsable = "Sistema LegalTech"
        });

        exp6.PlazosLegales.Add(new PlazoLegal
        {
            Id = Guid.NewGuid(),
            ExpedienteId = exp6.Id,
            Concepto = "Objeción de Fondo por Semejanza Fonética (Art. 89 LPI)",
            DiasHabiles = 60,
            FechaInicio = DateTime.Today.AddDays(-95),
            FechaVencimientoFatal = DateTime.Today.AddDays(-4), // Plazo expirado hace 4 días -> ⚫ Vencido (Mora)
            Cumplido = false
        });

        context.Expedientes.AddRange(exp1, exp2, exp3, exp4, exp5, exp6);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Obtiene la ficha de detalle completa de un expediente con su historial de actuaciones.
    /// </summary>
    public async Task<Expediente?> GetExpedienteDetalleAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.TipoSigno)
            .Include(e => e.EstadoProcesal)
            .Include(e => e.ViaPresentacion)
            .Include(e => e.Actuaciones.OrderByDescending(a => a.FechaActuacion))
                .ThenInclude(a => a.TipoActuacion)
            .Include(e => e.Publicaciones)
            .Include(e => e.Litigios)
            .Include(e => e.PlazosLegales)
            .Include(e => e.Certificado)
                .ThenInclude(c => c!.UbicacionArchivo)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    // -------------------------------------------------------------
    // CONSULTAS A TABLAS PARAMETRIZABLES (CATÁLOGOS EN BD)
    // -------------------------------------------------------------

    public async Task<List<Cliente>> GetClientesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Clientes.OrderBy(c => c.NombreRazonSocial).ToListAsync();
    }

    public async Task<List<TipoSigno>> GetTiposSignoAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TiposSigno.Where(t => t.Activo).OrderBy(t => t.Id).ToListAsync();
    }

    public async Task<List<EstadoProcesal>> GetEstadosProcesalesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EstadosProcesales.Where(ep => ep.Activo).OrderBy(ep => ep.Orden).ToListAsync();
    }

    public async Task<List<TipoActuacion>> GetTiposActuacionesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TiposActuaciones.Where(ta => ta.Activo).OrderBy(ta => ta.Id).ToListAsync();
    }

    public async Task<List<ViaPresentacion>> GetViasPresentacionAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ViasPresentacion.Where(vp => vp.Activo).OrderBy(vp => vp.Id).ToListAsync();
    }

    public async Task<List<UbicacionArchivo>> GetUbicacionesArchivoAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UbicacionesArchivo.Where(ua => ua.Activo).OrderBy(ua => ua.Id).ToListAsync();
    }

    // -------------------------------------------------------------
    // OPERACIONES TRANSACCIONALES
    // -------------------------------------------------------------

    /// <summary>
    /// Crea un expediente e inserta automáticamente su primera actuación en la línea de tiempo.
    /// </summary>
    public async Task<Expediente> CrearExpedienteAsync(Expediente nuevo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var anioActual = DateTime.UtcNow.Year;
        var totalExpedientesAnio = await context.Expedientes
            .CountAsync(e => e.CreadoEn.Year == anioActual) + 1;

        nuevo.CodigoInterno = $"EXP-{anioActual}-{totalExpedientesAnio:D4}";
        nuevo.CreadoEn = DateTime.UtcNow;
        nuevo.EstadoProcesalId = 1; // Recepción de Instrucciones por defecto

        // Crear la primera actuación en la línea de tiempo automáticamente
        nuevo.Actuaciones.Add(new ActuacionExpediente
        {
            TipoActuacionId = 1, // Ingreso de Instrucciones y Apertura
            Titulo = "Apertura de Expediente Físico y Digital",
            Comentario = $"Expediente creado en el sistema para la marca '{nuevo.DenominacionMarca}'.",
            FechaActuacion = DateTime.UtcNow,
            UsuarioResponsable = nuevo.ParalegalAsignado ?? "Paralegal Encargado"
        });

        context.Expedientes.Add(nuevo);
        await context.SaveChangesAsync();

        return nuevo;
    }

    /// <summary>
    /// Registra un nuevo hito en la línea de tiempo y opcionalmente avanza el estado procesal.
    /// </summary>
    public async Task RegistrarActuacionAsync(ActuacionExpediente actuacion, int? nuevoEstadoProcesalId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        actuacion.CreadoEn = DateTime.UtcNow;
        context.ActuacionesExpediente.Add(actuacion);

        if (nuevoEstadoProcesalId.HasValue)
        {
            var exp = await context.Expedientes.FindAsync(actuacion.ExpedienteId);
            if (exp != null)
            {
                exp.EstadoProcesalId = nuevoEstadoProcesalId.Value;
                exp.ActualizadoEn = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Actualiza la presentación formal ante DIGEPIH con número oficial y fecha de prioridad.
    /// </summary>
    public async Task RegistrarPresentacionOficialAsync(Guid expedienteId, string noExpedienteDIGEPIH, DateTime fechaPresentacion, int viaPresentacionId, string usuario)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var exp = await context.Expedientes.FindAsync(expedienteId);
        if (exp != null)
        {
            exp.NumeroExpedienteDIGEPIH = noExpedienteDIGEPIH;
            exp.FechaPresentacionOficial = fechaPresentacion;
            exp.ViaPresentacionId = viaPresentacionId;
            exp.EstadoProcesalId = 5; // Presentado ante DIGEPIH
            exp.ActualizadoEn = DateTime.UtcNow;

            context.ActuacionesExpediente.Add(new ActuacionExpediente
            {
                ExpedienteId = expedienteId,
                TipoActuacionId = 3, // Presentación Oficial
                Titulo = $"Presentación Oficial ante DIGEPIH - No. {noExpedienteDIGEPIH}",
                Comentario = $"Solicitud ingresada formalmente el {fechaPresentacion:dd/MM/yyyy}. Fija fecha de prioridad legal.",
                FechaActuacion = fechaPresentacion,
                UsuarioResponsable = usuario
            });

            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Guarda o actualiza las publicaciones en La Gaceta (ENAG).
    /// </summary>
    public async Task GuardarPublicacionENAGAsync(PublicacionENAG publicacion, string usuario)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        if (publicacion.Id == Guid.Empty)
        {
            publicacion.Id = Guid.NewGuid();
            context.PublicacionesENAG.Add(publicacion);
        }
        else
        {
            context.PublicacionesENAG.Update(publicacion);
        }

        // Si se marcaron los ejemplares físicos entregados a DIGEPIH, registrar la actuación fatal
        if (publicacion.EjemplaresEntregadosDIGEPIH && publicacion.FechaEntregaEjemplaresDIGEPIH.HasValue)
        {
            var exp = await context.Expedientes.FindAsync(publicacion.ExpedienteId);
            if (exp != null)
            {
                exp.EstadoProcesalId = 15; // Ejemplares Físicos Entregados IP
                exp.ActualizadoEn = DateTime.UtcNow;
            }

            context.ActuacionesExpediente.Add(new ActuacionExpediente
            {
                ExpedienteId = publicacion.ExpedienteId,
                TipoActuacionId = 10, // Presentación de 3 Ejemplares a DIGEPIH
                Titulo = "Presentación de los 3 Ejemplares de La Gaceta ante DIGEPIH",
                Comentario = "Se entregaron los tres ejemplares físicos de publicación en ventanilla para evitar el abandono de la marca.",
                FechaActuacion = publicacion.FechaEntregaEjemplaresDIGEPIH.Value,
                UsuarioResponsable = usuario
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Actualiza los datos generales de un expediente existente.
    /// </summary>
    public async Task ActualizarExpedienteAsync(Expediente expediente)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existente = await context.Expedientes.FindAsync(expediente.Id);
        if (existente != null)
        {
            existente.DenominacionMarca = expediente.DenominacionMarca;
            existente.ClienteId = expediente.ClienteId;
            existente.TipoSignoId = expediente.TipoSignoId;
            existente.ClaseNiza = expediente.ClaseNiza;
            existente.DescripcionEspecificaProductosServicios = expediente.DescripcionEspecificaProductosServicios;
            existente.NumeroExpedienteDIGEPIH = expediente.NumeroExpedienteDIGEPIH;
            existente.FechaPresentacionOficial = expediente.FechaPresentacionOficial;
            existente.ViaPresentacionId = expediente.ViaPresentacionId;
            existente.ReivindicaPrioridad = expediente.ReivindicaPrioridad;
            existente.PaisPrioridad = expediente.PaisPrioridad;
            existente.FechaPrioridad = expediente.FechaPrioridad;
            existente.NumeroSolicitudPrioridad = expediente.NumeroSolicitudPrioridad;
            existente.TimbreL50Pagado = expediente.TimbreL50Pagado;
            existente.EtiquetasFisicas2x4Entregadas = expediente.EtiquetasFisicas2x4Entregadas;
            existente.TasasOficialesPagadas = expediente.TasasOficialesPagadas;
            existente.AbogadoResponsable = expediente.AbogadoResponsable;
            existente.ParalegalAsignado = expediente.ParalegalAsignado;
            existente.ActualizadoEn = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Cambia el estado procesal y agrega una nota automática a la bitácora.
    /// </summary>
    public async Task ActualizarEstadoProcesalAsync(Guid expedienteId, int nuevoEstadoId, string? comentario = null, string usuario = "Sistema")
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var exp = await context.Expedientes
            .Include(e => e.EstadoProcesal)
            .FirstOrDefaultAsync(e => e.Id == expedienteId);

        var nuevoEstado = await context.EstadosProcesales.FindAsync(nuevoEstadoId);
        if (exp != null && nuevoEstado != null)
        {
            var estadoAnterior = exp.EstadoProcesal?.Nombre ?? "Sin estado";
            exp.EstadoProcesalId = nuevoEstadoId;
            exp.ActualizadoEn = DateTime.UtcNow;

            context.ActuacionesExpediente.Add(new ActuacionExpediente
            {
                ExpedienteId = expedienteId,
                TipoActuacionId = 15, // Nota o Memorándum Interno
                Titulo = $"Cambio de Estado Procesal: {nuevoEstado.Nombre}",
                Comentario = comentario ?? $"Transición de estado de '{estadoAnterior}' a '{nuevoEstado.Nombre}'.",
                FechaActuacion = DateTime.UtcNow,
                UsuarioResponsable = usuario
            });

            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Guarda o actualiza el Título / Certificado de Registro de Marca.
    /// </summary>
    public async Task GuardarCertificadoAsync(CertificadoRegistro cert, string usuario)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existente = await context.Certificados.FirstOrDefaultAsync(c => c.ExpedienteId == cert.ExpedienteId);

        if (existente == null)
        {
            if (cert.Id == Guid.Empty) cert.Id = Guid.NewGuid();
            context.Certificados.Add(cert);

            // Registrar actuación de emisión de certificado
            context.ActuacionesExpediente.Add(new ActuacionExpediente
            {
                ExpedienteId = cert.ExpedienteId,
                TipoActuacionId = 13, // Certificado de Registro Custodiado
                Titulo = $"Certificado Oficial de Registro No. {cert.NumeroRegistroOficial}",
                Comentario = $"Título registrado con vigencia decenal hasta el {cert.FechaVencimientoDecenal:dd/MM/yyyy}.",
                FechaActuacion = cert.FechaConcesion != default ? cert.FechaConcesion : DateTime.UtcNow,
                UsuarioResponsable = usuario
            });

            // Actualizar estado procesal del expediente a Certificado Emitido (ID 19)
            var exp = await context.Expedientes.FindAsync(cert.ExpedienteId);
            if (exp != null)
            {
                exp.EstadoProcesalId = 19; // Certificado de Registro Emitido
                exp.ActualizadoEn = DateTime.UtcNow;
            }
        }
        else
        {
            existente.NumeroRegistroOficial = cert.NumeroRegistroOficial;
            existente.FechaConcesion = cert.FechaConcesion;
            existente.FechaVencimientoDecenal = cert.FechaVencimientoDecenal;
            existente.UbicacionArchivoId = cert.UbicacionArchivoId;
            existente.FechaEntregaCliente = cert.FechaEntregaCliente;
            existente.FirmaRecepcionClienteUri = cert.FirmaRecepcionClienteUri;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Compuerta de Solvencia P360 (Gatekeeper Financiero de Honduras).
    /// </summary>
    public async Task ValidarSolvenciaAsync(Guid expedienteId, bool solvencia, string usuario)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cert = await context.Certificados.FirstOrDefaultAsync(c => c.ExpedienteId == expedienteId);
        if (cert != null)
        {
            cert.SolvenciaValidada = solvencia;
            cert.FechaValidacionSolvencia = solvencia ? DateTime.UtcNow : null;
            cert.ValidadoPorUsuario = solvencia ? usuario : null;

            context.ActuacionesExpediente.Add(new ActuacionExpediente
            {
                ExpedienteId = expedienteId,
                TipoActuacionId = solvencia ? 14 : 15,
                Titulo = solvencia ? "Solvencia P360 Aprobada para Despacho" : "Solvencia P360 Revocada",
                Comentario = solvencia 
                    ? $"El usuario '{usuario}' validó la solvencia fiscal y de honorarios. Se autoriza la entrega del título original al cliente."
                    : $"El usuario '{usuario}' revocó la solvencia. Se bloquea la entrega física y digital del título.",
                FechaActuacion = DateTime.UtcNow,
                UsuarioResponsable = usuario
            });

            await context.SaveChangesAsync();
        }
    }
}
