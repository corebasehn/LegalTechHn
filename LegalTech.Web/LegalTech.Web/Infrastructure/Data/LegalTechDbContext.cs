using Microsoft.EntityFrameworkCore;
using LegalTech.Web.Domain.Entities;

namespace LegalTech.Web.Infrastructure.Data;

/// <summary>
/// Contexto principal de Entity Framework Core para LegalTech Honduras.
/// </summary>
public class LegalTechDbContext : DbContext
{
    public LegalTechDbContext(DbContextOptions<LegalTechDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<TipoSigno> TiposSigno => Set<TipoSigno>();
    public DbSet<EstadoProcesal> EstadosProcesales => Set<EstadoProcesal>();
    public DbSet<TipoActuacion> TiposActuaciones => Set<TipoActuacion>();
    public DbSet<ViaPresentacion> ViasPresentacion => Set<ViaPresentacion>();
    public DbSet<UbicacionArchivo> UbicacionesArchivo => Set<UbicacionArchivo>();
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<ActuacionExpediente> ActuacionesExpediente => Set<ActuacionExpediente>();
    public DbSet<PublicacionENAG> PublicacionesENAG => Set<PublicacionENAG>();
    public DbSet<Litigio> Litigios => Set<Litigio>();
    public DbSet<CertificadoRegistro> Certificados => Set<CertificadoRegistro>();
    public DbSet<PlazoLegal> PlazosLegales => Set<PlazoLegal>();
    public DbSet<FeriadoNacional> FeriadosNacionales => Set<FeriadoNacional>();
    public DbSet<ParametroFiscal> ParametrosFiscales => Set<ParametroFiscal>();
    public DbSet<TarifaArancelaria> TarifasArancelarias => Set<TarifaArancelaria>();
    public DbSet<FacturaCobro> Facturas => Set<FacturaCobro>();
    public DbSet<DetalleFacturaCobro> DetallesFactura => Set<DetalleFacturaCobro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cliente (Titular)
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NombreRazonSocial).IsRequired().HasMaxLength(250);
            entity.Property(c => c.NumeroIdentificacionRTN).IsRequired().HasMaxLength(50);
            entity.Property(c => c.CorreoElectronico).HasMaxLength(150);
            entity.Property(c => c.Telefono).HasMaxLength(50);
            entity.Property(c => c.Nacionalidad).HasMaxLength(100);
        });

        // 1. Catálogo de Tipos de Signos Distintivos (Parametrizable)
        modelBuilder.Entity<TipoSigno>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Descripcion).HasMaxLength(250);

            entity.HasData(
                new TipoSigno { Id = 1, Nombre = "Marca de Fábrica", Descripcion = "Signo para distinguir productos (Clases 1 a 34)", Activo = true },
                new TipoSigno { Id = 2, Nombre = "Marca de Servicios", Descripcion = "Signo para distinguir servicios (Clases 35 a 45)", Activo = true },
                new TipoSigno { Id = 3, Nombre = "Denominativa", Descripcion = "Signo compuesto exclusivamente por palabras, letras o números", Activo = true },
                new TipoSigno { Id = 4, Nombre = "Figurativa", Descripcion = "Signo integrado únicamente por un gráfico, logotipo o figura", Activo = true },
                new TipoSigno { Id = 5, Nombre = "Mixta", Descripcion = "Combinación de elementos denominativos y gráficos", Activo = true },
                new TipoSigno { Id = 6, Nombre = "Lema Comercial", Descripcion = "Frase o leyenda publicitaria asociada a una marca previa", Activo = true },
                new TipoSigno { Id = 7, Nombre = "Nombre Comercial", Descripcion = "Signo que identifica un establecimiento o empresa", Activo = true },
                new TipoSigno { Id = 8, Nombre = "Emblema", Descripcion = "Signo gráfico representativo de una empresa", Activo = true },
                new TipoSigno { Id = 9, Nombre = "Marca Colectiva", Descripcion = "Signo que distingue el origen de asociaciones o cooperativas", Activo = true },
                new TipoSigno { Id = 10, Nombre = "Marca de Certificación", Descripcion = "Garantiza normas de calidad, origen o procesos", Activo = true },
                new TipoSigno { Id = 11, Nombre = "Denominación de Origen / Ind. Geográfica", Descripcion = "Signo vinculado a una región geográfica específica", Activo = true }
            );
        });

        // 2. Catálogo de Estados Procesales (Parametrizable)
        modelBuilder.Entity<EstadoProcesal>(entity =>
        {
            entity.HasKey(ep => ep.Id);
            entity.Property(ep => ep.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(ep => ep.Fase).HasMaxLength(50);
            entity.Property(ep => ep.ColorBadge).HasMaxLength(30);

            entity.HasData(
                new EstadoProcesal { Id = 1, Nombre = "Recepción de Instrucciones", Fase = "Apertura", ColorBadge = "primary", Orden = 1, Activo = true },
                new EstadoProcesal { Id = 2, Nombre = "Búsqueda Preliminar y Viabilidad", Fase = "Apertura", ColorBadge = "info", Orden = 2, Activo = true },
                new EstadoProcesal { Id = 3, Nombre = "Dictamen Emitido al Cliente", Fase = "Apertura", ColorBadge = "info", Orden = 3, Activo = true },
                new EstadoProcesal { Id = 4, Nombre = "Preparación de Expediente", Fase = "Preparación", ColorBadge = "primary", Orden = 4, Activo = true },
                new EstadoProcesal { Id = 5, Nombre = "Presentado ante DIGEPIH", Fase = "Presentación", ColorBadge = "success", Orden = 5, Activo = true },
                new EstadoProcesal { Id = 6, Nombre = "En Examen de Forma", Fase = "Examen", ColorBadge = "warning", Orden = 6, Activo = true },
                new EstadoProcesal { Id = 7, Nombre = "Con Prevención de Forma (30 días)", Fase = "Examen", ColorBadge = "warning", Orden = 7, Activo = true },
                new EstadoProcesal { Id = 8, Nombre = "Abandono por Forma", Fase = "Examen", ColorBadge = "danger", Orden = 8, Activo = true },
                new EstadoProcesal { Id = 9, Nombre = "En Examen de Fondo", Fase = "Examen", ColorBadge = "warning", Orden = 9, Activo = true },
                new EstadoProcesal { Id = 10, Nombre = "Con Objeción de Fondo (60 días)", Fase = "Examen", ColorBadge = "danger", Orden = 10, Activo = true },
                new EstadoProcesal { Id = 11, Nombre = "Contestación de Fondo Presentada", Fase = "Examen", ColorBadge = "info", Orden = 11, Activo = true },
                new EstadoProcesal { Id = 12, Nombre = "Denegatoria Definitiva", Fase = "Examen", ColorBadge = "danger", Orden = 12, Activo = true },
                new EstadoProcesal { Id = 13, Nombre = "Aviso de Publicación Emitido", Fase = "Publicación", ColorBadge = "success", Orden = 13, Activo = true },
                new EstadoProcesal { Id = 14, Nombre = "En Publicación La Gaceta (3 avisos)", Fase = "Publicación", ColorBadge = "info", Orden = 14, Activo = true },
                new EstadoProcesal { Id = 15, Nombre = "Ejemplares Físicos Entregados IP", Fase = "Publicación", ColorBadge = "success", Orden = 15, Activo = true },
                new EstadoProcesal { Id = 16, Nombre = "En Período de Oposición (30 días)", Fase = "Oposición", ColorBadge = "warning", Orden = 16, Activo = true },
                new EstadoProcesal { Id = 17, Nombre = "Con Oposición de Tercero (10 días)", Fase = "Oposición", ColorBadge = "danger", Orden = 17, Activo = true },
                new EstadoProcesal { Id = 18, Nombre = "Resolución de Concesión", Fase = "Concesión", ColorBadge = "success", Orden = 18, Activo = true },
                new EstadoProcesal { Id = 19, Nombre = "Certificado de Registro Emitido", Fase = "Concesión", ColorBadge = "success", Orden = 19, Activo = true },
                new EstadoProcesal { Id = 20, Nombre = "Certificado Entregado al Cliente", Fase = "Concesión", ColorBadge = "success", Orden = 20, Activo = true },
                new EstadoProcesal { Id = 21, Nombre = "En Vigilancia Decenal", Fase = "Vigilancia", ColorBadge = "primary", Orden = 21, Activo = true }
            );
        });

        // 3. Catálogo de Tipos de Actuaciones (Línea de Tiempo)
        modelBuilder.Entity<TipoActuacion>(entity =>
        {
            entity.HasKey(ta => ta.Id);
            entity.Property(ta => ta.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(ta => ta.Icono).HasMaxLength(50);
            entity.Property(ta => ta.Color).HasMaxLength(30);

            entity.HasData(
                new TipoActuacion { Id = 1, Nombre = "Ingreso de Instrucciones y Apertura", Icono = "folder_open", Color = "primary", Descripcion = "Cliente remite instrucciones e inicia el expediente", Activo = true },
                new TipoActuacion { Id = 2, Nombre = "Búsqueda Registral de Antecedentes", Icono = "search", Color = "info", Descripcion = "Análisis fonético, ortográfico y visual en BD del IP", Activo = true },
                new TipoActuacion { Id = 3, Nombre = "Presentación Oficial ante DIGEPIH", Icono = "upload_file", Color = "success", Descripcion = "Ingreso de solicitud en ventanilla y fijación de prioridad", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 4, Nombre = "Auto de Prevención de Forma", Icono = "warning", Color = "warning", Descripcion = "Notificación oficial de DIGEPIH otorgando 30 días hábiles", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 5, Nombre = "Escrito de Subsanación Presentado", Icono = "check_circle", Color = "success", Descripcion = "Evacuación de prevención formal en tiempo y forma", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 6, Nombre = "Auto de Objeción de Fondo (Denegatoria)", Icono = "report_problem", Color = "danger", Descripcion = "Objeción por Arts. 83 y 84 LPI con 60 días hábiles", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 7, Nombre = "Escrito de Contestación de Fondo", Icono = "gavel", Color = "info", Descripcion = "Defensa jurídica y técnica de registrabilidad", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 8, Nombre = "Emisión de Aviso de Publicación", Icono = "newspaper", Color = "success", Descripcion = "DIGEPIH aprueba solicitud para publicar en La Gaceta", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 9, Nombre = "Aviso Publicado en La Gaceta (ENAG)", Icono = "feed", Color = "info", Descripcion = "Constancia de publicación oficial en diario nacional", Activo = true },
                new TipoActuacion { Id = 10, Nombre = "Presentación de 3 Ejemplares a DIGEPIH", Icono = "inventory_2", Color = "success", Descripcion = "Cumplimiento fatal de entrega de diarios físicos", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 11, Nombre = "Notificación de Oposición de Tercero", Icono = "security", Color = "danger", Descripcion = "Tercero formula oposición (10 días para contestar)", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 12, Nombre = "Resolución Final de Concesión", Icono = "verified", Color = "success", Descripcion = "Resolución que concede el registro de marca", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 13, Nombre = "Certificado de Registro Custodiado", Icono = "workspace_premium", Color = "success", Descripcion = "Recepción de título oficial emitido por DIGEPIH", RequiereDocumentoAdjunto = true, Activo = true },
                new TipoActuacion { Id = 14, Nombre = "Entrega de Título al Cliente (Solvente)", Icono = "handshake", Color = "primary", Descripcion = "Despacho final del certificado tras validar solvencia", Activo = true },
                new TipoActuacion { Id = 15, Nombre = "Nota o Memorándum Interno", Icono = "note", Color = "secondary", Descripcion = "Anotación administrativa de los abogados o paralegales", Activo = true }
            );
        });

        // 4. Catálogo de Vías de Presentación (Parametrizable)
        modelBuilder.Entity<ViaPresentacion>(entity =>
        {
            entity.HasKey(vp => vp.Id);
            entity.Property(vp => vp.Nombre).IsRequired().HasMaxLength(150);

            entity.HasData(
                new ViaPresentacion { Id = 1, Nombre = "Ventanilla Presencial DIGEPIH - Tegucigalpa (Centro Cívico)", Descripcion = "Presentación física con sello de recepción", Activo = true },
                new ViaPresentacion { Id = 2, Nombre = "Ventanilla Presencial DIGEPIH - San Pedro Sula", Descripcion = "Presentación física regional", Activo = true },
                new ViaPresentacion { Id = 3, Nombre = "Portal Electrónico en Línea DIGEPIH", Descripcion = "Presentación digital autorizada", Activo = true }
            );
        });

        // 5. Catálogo de Ubicaciones Físicas de Archivo (Parametrizable)
        modelBuilder.Entity<UbicacionArchivo>(entity =>
        {
            entity.HasKey(ua => ua.Id);
            entity.Property(ua => ua.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(ua => ua.CodigoArea).HasMaxLength(50);

            entity.HasData(
                new UbicacionArchivo { Id = 1, Nombre = "Archivo Activo - Trámites Marcarios en Curso", CodigoArea = "EST-ACT-01", Descripcion = "Carpetas en trámite de examen o publicación", Activo = true },
                new UbicacionArchivo { Id = 2, Nombre = "Archivo Especial - Litigios y Oposiciones", CodigoArea = "GAV-LIT-02", Descripcion = "Expedientes bajo litigio o recursos", Activo = true },
                new UbicacionArchivo { Id = 3, Nombre = "Bóveda de Seguridad - Certificados y Títulos Originales", CodigoArea = "BOV-TIT-01", Descripcion = "Custodia de títulos originales bajo llave", Activo = true },
                new UbicacionArchivo { Id = 4, Nombre = "Archivo Pasivo Histórico (Marcas Concluidas)", CodigoArea = "PAS-HIS-03", Descripcion = "Expedientes entregados o archivados", Activo = true }
            );
        });

        // Expediente Marcario
        modelBuilder.Entity<Expediente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodigoInterno).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.CodigoInterno).IsUnique();

            entity.Property(e => e.DenominacionMarca).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NumeroExpedienteDIGEPIH).HasMaxLength(50);

            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Expedientes)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TipoSigno)
                  .WithMany()
                  .HasForeignKey(e => e.TipoSignoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EstadoProcesal)
                  .WithMany()
                  .HasForeignKey(e => e.EstadoProcesalId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ViaPresentacion)
                  .WithMany()
                  .HasForeignKey(e => e.ViaPresentacionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Certificado)
                  .WithOne(c => c.Expediente)
                  .HasForeignKey<CertificadoRegistro>(c => c.ExpedienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Actuaciones de la Línea de Tiempo
        modelBuilder.Entity<ActuacionExpediente>(entity =>
        {
            entity.HasKey(ae => ae.Id);
            entity.Property(ae => ae.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(ae => ae.UsuarioResponsable).HasMaxLength(100);

            entity.HasOne(ae => ae.Expediente)
                  .WithMany(e => e.Actuaciones)
                  .HasForeignKey(ae => ae.ExpedienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ae => ae.TipoActuacion)
                  .WithMany()
                  .HasForeignKey(ae => ae.TipoActuacionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Publicaciones La Gaceta (ENAG)
        modelBuilder.Entity<PublicacionENAG>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Expediente)
                  .WithMany(e => e.Publicaciones)
                  .HasForeignKey(p => p.ExpedienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Litigios y Controversias
        modelBuilder.Entity<Litigio>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ParteDemandante).HasMaxLength(200);
            entity.Property(l => l.ParteDemandada).HasMaxLength(200);
            entity.Property(l => l.MarcaEnConflicto).HasMaxLength(200);

            entity.HasOne(l => l.Expediente)
                  .WithMany(e => e.Litigios)
                  .HasForeignKey(l => l.ExpedienteId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Certificado de Registro
        modelBuilder.Entity<CertificadoRegistro>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NumeroRegistroOficial).IsRequired().HasMaxLength(50);

            entity.HasOne(c => c.UbicacionArchivo)
                  .WithMany()
                  .HasForeignKey(c => c.UbicacionArchivoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Plazos Legales
        modelBuilder.Entity<PlazoLegal>(entity =>
        {
            entity.HasKey(pl => pl.Id);
            entity.Property(pl => pl.Concepto).IsRequired().HasMaxLength(150);

            entity.HasOne(pl => pl.Expediente)
                  .WithMany(e => e.PlazosLegales)
                  .HasForeignKey(pl => pl.ExpedienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Feriados y Asuetos Nacionales de Honduras (Art. 42 LPA)
        modelBuilder.Entity<FeriadoNacional>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(f => f.FundamentoLegal).HasMaxLength(250);

            entity.HasData(
                // Feriados Fijos Anuales (Art. 339 Código del Trabajo)
                new FeriadoNacional { Id = 1, Fecha = new DateTime(2026, 1, 1), Nombre = "Año Nuevo", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
                new FeriadoNacional { Id = 2, Fecha = new DateTime(2026, 4, 14), Nombre = "Día de las Américas", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
                new FeriadoNacional { Id = 3, Fecha = new DateTime(2026, 5, 1), Nombre = "Día Internacional del Trabajo", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
                new FeriadoNacional { Id = 4, Fecha = new DateTime(2026, 9, 15), Nombre = "Día de la Independencia Nacional", FundamentoLegal = "Fiesta Cívica Nacional", EsFijoAnual = true, Activo = true },
                new FeriadoNacional { Id = 5, Fecha = new DateTime(2026, 12, 25), Nombre = "Navidad", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },

                // Asuetos Móviles 2025
                new FeriadoNacional { Id = 6, Fecha = new DateTime(2025, 4, 17), Nombre = "Jueves Santo 2025", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 7, Fecha = new DateTime(2025, 4, 18), Nombre = "Viernes Santo 2025", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 8, Fecha = new DateTime(2025, 10, 1), Nombre = "Feriado Morazánico 2025 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 9, Fecha = new DateTime(2025, 10, 2), Nombre = "Feriado Morazánico 2025 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 10, Fecha = new DateTime(2025, 10, 3), Nombre = "Feriado Morazánico 2025 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },

                // Asuetos Móviles 2026
                new FeriadoNacional { Id = 11, Fecha = new DateTime(2026, 4, 2), Nombre = "Jueves Santo 2026", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 12, Fecha = new DateTime(2026, 4, 3), Nombre = "Viernes Santo 2026", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 13, Fecha = new DateTime(2026, 10, 7), Nombre = "Feriado Morazánico 2026 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 14, Fecha = new DateTime(2026, 10, 8), Nombre = "Feriado Morazánico 2026 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 15, Fecha = new DateTime(2026, 10, 9), Nombre = "Feriado Morazánico 2026 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },

                // Asuetos Móviles 2027
                new FeriadoNacional { Id = 16, Fecha = new DateTime(2027, 3, 25), Nombre = "Jueves Santo 2027", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 17, Fecha = new DateTime(2027, 3, 26), Nombre = "Viernes Santo 2027", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 18, Fecha = new DateTime(2027, 10, 6), Nombre = "Feriado Morazánico 2027 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 19, Fecha = new DateTime(2027, 10, 7), Nombre = "Feriado Morazánico 2027 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
                new FeriadoNacional { Id = 20, Fecha = new DateTime(2027, 10, 8), Nombre = "Feriado Morazánico 2027 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true }
            );
        });

        // 7. Catálogo de Parámetros Fiscales y Tasas Oficiales (Parametrizable)
        modelBuilder.Entity<ParametroFiscal>(entity =>
        {
            entity.HasKey(pf => pf.Id);
            entity.Property(pf => pf.Clave).IsRequired().HasMaxLength(100);
            entity.Property(pf => pf.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(pf => pf.Valor).IsRequired().HasMaxLength(200);
            entity.Property(pf => pf.TipoDato).HasMaxLength(50);
            entity.Property(pf => pf.Categoria).HasMaxLength(100);

            entity.HasData(
                new ParametroFiscal { Id = 1, Clave = "ISV_PORCENTAJE", Nombre = "Porcentaje Impuesto sobre Ventas (ISV)", Valor = "15.00", TipoDato = "decimal", Categoria = "Impuestos", Descripcion = "Tasa general del 15% sobre honorarios profesionales de servicios legales", Activo = true },
                new ParametroFiscal { Id = 2, Clave = "TASA_CAMBIO_USD_HNL", Nombre = "Tasa de Cambio Oficial (USD a HNL)", Valor = "25.50", TipoDato = "decimal", Categoria = "Divisas", Descripcion = "Tipo de cambio de referencia del Banco Central de Honduras (BCH)", Activo = true },
                new ParametroFiscal { Id = 3, Clave = "TIMBRE_CONTRATACION_HNL", Nombre = "Timbre de Contratación (Colegio de Abogados)", Valor = "50.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Timbre obligatorio del CAH por cada solicitud presentada en ventanilla", Activo = true },
                new ParametroFiscal { Id = 4, Clave = "TASA_SOLICITUD_DIGEPIH_HNL", Nombre = "Tasa Oficial de Presentación DIGEPIH", Valor = "700.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Tasa gubernamental de radicación de solicitud por cada clase Niza", Activo = true },
                new ParametroFiscal { Id = 5, Clave = "TASA_REGISTRO_TITULO_HNL", Nombre = "Tasa Oficial de Emisión de Certificado", Valor = "500.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Tasa oficial ante DIGEPIH por emisión del título de concesión", Activo = true },
                new ParametroFiscal { Id = 6, Clave = "TASA_PUBLICACION_ENAG_HNL", Nombre = "Tasa de Publicación en La Gaceta (ENAG)", Valor = "650.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Pago a la Empresa Nacional de Artes Gráficas por los 3 avisos de ley", Activo = true },
                new ParametroFiscal { Id = 7, Clave = "CAI_SAR_AUTORIZADO", Nombre = "Código de Autorización de Impresión (CAI)", Valor = "A84F2B-98CE21-1B4480-1498B2-FA8321-44", TipoDato = "string", Categoria = "Facturación SAR", Descripcion = "Régimen de facturación autorizado por el Servicio de Administración de Rentas", Activo = true },
                new ParametroFiscal { Id = 8, Clave = "FECHA_LIMITE_SAR", Nombre = "Fecha Límite de Emisión de Facturas", Valor = "2027-12-31", TipoDato = "date", Categoria = "Facturación SAR", Descripcion = "Fecha máxima de vigencia del rango de facturación asignado por SAR", Activo = true }
            );
        });

        // 8. Catálogo de Tarifas Arancelarias y Honorarios Profesionales (Parametrizable)
        modelBuilder.Entity<TarifaArancelaria>(entity =>
        {
            entity.HasKey(ta => ta.Id);
            entity.Property(ta => ta.Concepto).IsRequired().HasMaxLength(250);
            entity.Property(ta => ta.EtapaProcesal).IsRequired().HasMaxLength(100);
            entity.Property(ta => ta.HonorariosHNL).HasPrecision(18, 2);
            entity.Property(ta => ta.HonorariosUSD).HasPrecision(18, 2);
            entity.Property(ta => ta.GastosOficialesHNL).HasPrecision(18, 2);
            entity.Property(ta => ta.GastosOficialesUSD).HasPrecision(18, 2);

            entity.HasData(
                new TarifaArancelaria { Id = 1, Concepto = "Búsqueda Preliminar de Antecedentes y Dictamen", EtapaProcesal = "Apertura", HonorariosHNL = 2500.00m, HonorariosUSD = 100.00m, GastosOficialesHNL = 0.00m, GastosOficialesUSD = 0.00m, AplicaISV = true, Descripcion = "Análisis fonético, visual y conceptual previo en base de datos DIGEPIH", Activo = true },
                new TarifaArancelaria { Id = 2, Concepto = "Solicitud y Trámite Completo de Registro (por Clase Niza)", EtapaProcesal = "Presentación", HonorariosHNL = 8500.00m, HonorariosUSD = 350.00m, GastosOficialesHNL = 1400.00m, GastosOficialesUSD = 55.00m, AplicaISV = true, Descripcion = "Incluye radicación, timbres CAH (L 50), tasa DIGEPIH (L 700) y La Gaceta (L 650)", Activo = true },
                new TarifaArancelaria { Id = 3, Concepto = "Contestación de Prevención de Forma (30 días)", EtapaProcesal = "Examen", HonorariosHNL = 3500.00m, HonorariosUSD = 140.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Subsanación de requisitos formales, clasificación de Niza o poderes", Activo = true },
                new TarifaArancelaria { Id = 4, Concepto = "Contestación de Objeción de Fondo (60 días)", EtapaProcesal = "Examen", HonorariosHNL = 7000.00m, HonorariosUSD = 280.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Defensa jurídica ante reparos de distintividad o semejanza (Arts. 83 y 84 LPI)", Activo = true },
                new TarifaArancelaria { Id = 5, Concepto = "Gestión de Publicaciones y Depósito de Diarios Físicos", EtapaProcesal = "Publicación", HonorariosHNL = 2000.00m, HonorariosUSD = 80.00m, GastosOficialesHNL = 650.00m, GastosOficialesUSD = 25.00m, AplicaISV = true, Descripcion = "Seguimiento a 3 avisos ENAG y entrega de ejemplares físicos para evitar abandono", Activo = true },
                new TarifaArancelaria { Id = 6, Concepto = "Defensa ante Oposición de Terceros (10 días)", EtapaProcesal = "Oposición", HonorariosHNL = 10000.00m, HonorariosUSD = 400.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Contestación, evacuación de medios probatorios y conclusiones de primera instancia", Activo = true },
                new TarifaArancelaria { Id = 7, Concepto = "Interposición de Oposición en Ataque (30 días)", EtapaProcesal = "Oposición", HonorariosHNL = 12000.00m, HonorariosUSD = 480.00m, GastosOficialesHNL = 500.00m, GastosOficialesUSD = 20.00m, AplicaISV = true, Descripcion = "Oposición contra solicitud lesiva de tercero publicada en La Gaceta", Activo = true },
                new TarifaArancelaria { Id = 8, Concepto = "Concesión, Certificado Oficial y Solvencia P360", EtapaProcesal = "Concesión", HonorariosHNL = 3000.00m, HonorariosUSD = 120.00m, GastosOficialesHNL = 500.00m, GastosOficialesUSD = 20.00m, AplicaISV = true, Descripcion = "Retiro de certificado original de DIGEPIH, auditoría P360 y custodia en bóveda", Activo = true },
                new TarifaArancelaria { Id = 9, Concepto = "Acción de Cancelación por No Uso (3 años)", EtapaProcesal = "Litigios", HonorariosHNL = 12500.00m, HonorariosUSD = 500.00m, GastosOficialesHNL = 750.00m, GastosOficialesUSD = 30.00m, AplicaISV = true, Descripcion = "Auditoría en redes sociales, constancia de no rehabilitación y demanda de cancelación", Activo = true },
                new TarifaArancelaria { Id = 10, Concepto = "Acción de Nulidad de Registro Marcario", EtapaProcesal = "Litigios", HonorariosHNL = 15000.00m, HonorariosUSD = 600.00m, GastosOficialesHNL = 1000.00m, GastosOficialesUSD = 40.00m, AplicaISV = true, Descripcion = "Demanda por registro otorgado en contravención a la Ley de Propiedad Industrial", Activo = true },
                new TarifaArancelaria { Id = 11, Concepto = "Recurso de Reposición (10 días) / Apelación (3 días)", EtapaProcesal = "Litigios", HonorariosHNL = 8000.00m, HonorariosUSD = 320.00m, GastosOficialesHNL = 100.00m, GastosOficialesUSD = 4.00m, AplicaISV = true, Descripcion = "Impugnación administrativa ante DIGEPIH y Superintendencia de Recursos", Activo = true }
            );
        });

        // Facturación y Cobranzas
        modelBuilder.Entity<FacturaCobro>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.NumeroFactura).IsRequired().HasMaxLength(50);
            entity.Property(f => f.NumeroCAI).HasMaxLength(100);
            entity.Property(f => f.Moneda).IsRequired().HasMaxLength(10);
            entity.Property(f => f.TasaCambio).HasPrecision(18, 4);
            entity.Property(f => f.SubtotalHonorarios).HasPrecision(18, 2);
            entity.Property(f => f.SubtotalGastosOficiales).HasPrecision(18, 2);
            entity.Property(f => f.MontoISV).HasPrecision(18, 2);
            entity.Property(f => f.TotalFactura).HasPrecision(18, 2);
            entity.Property(f => f.MontoPagado).HasPrecision(18, 2);
            entity.Property(f => f.SaldoPendiente).HasPrecision(18, 2);

            entity.HasOne(f => f.Cliente)
                  .WithMany()
                  .HasForeignKey(f => f.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Expediente)
                  .WithMany()
                  .HasForeignKey(f => f.ExpedienteId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(f => f.Detalles)
                  .WithOne(d => d.Factura)
                  .HasForeignKey(d => d.FacturaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DetalleFacturaCobro>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Concepto).IsRequired().HasMaxLength(250);
            entity.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(d => d.Subtotal).HasPrecision(18, 2);
            entity.Property(d => d.MontoISV).HasPrecision(18, 2);
            entity.Property(d => d.TotalLinea).HasPrecision(18, 2);

            entity.HasOne(d => d.TarifaArancelaria)
                  .WithMany()
                  .HasForeignKey(d => d.TarifaArancelariaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
