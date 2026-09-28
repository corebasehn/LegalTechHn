using LegalTech.Web.Components;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Domain.Enums;
using LegalTech.Web.Infrastructure.Data;
using LegalTech.Web.Infrastructure.Security;
using LegalTech.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Soporte para puertos dinámicos en la nube (Render, Railway, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

// Soporte de Seguridad, Autenticación y Autorización RBAC basada en Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LegalTech.Session";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acceso-denegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthService>();

// Configuración de Entity Framework Core con SQLite y Factory para Blazor
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=legaltech.db";

builder.Services.AddDbContextFactory<LegalTechDbContext>(options =>
    options.UseSqlite(connectionString));

// Registrar servicios del dominio
builder.Services.AddScoped<ExpedienteService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<PlazosService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<LitigioService>();
builder.Services.AddScoped<FacturacionService>();
builder.Services.AddScoped<ConfiguracionService>();
builder.Services.AddScoped<DocumentoService>();

var app = builder.Build();

// Garantizar que la base de datos SQLite y todas sus tablas se creen automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LegalTechDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();

    // Asegurar compatibilidad y creación de tabla FeriadosNacionales si la base SQLite ya existía
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""FeriadosNacionales"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_FeriadosNacionales"" PRIMARY KEY AUTOINCREMENT,
            ""Fecha"" TEXT NOT NULL,
            ""Nombre"" TEXT NOT NULL,
            ""FundamentoLegal"" TEXT NULL,
            ""EsFijoAnual"" INTEGER NOT NULL,
            ""Activo"" INTEGER NOT NULL
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""PlazosLegales"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_PlazosLegales"" PRIMARY KEY,
            ""ExpedienteId"" TEXT NOT NULL,
            ""Concepto"" TEXT NOT NULL,
            ""DiasHabiles"" INTEGER NOT NULL,
            ""FechaInicio"" TEXT NOT NULL,
            ""FechaVencimientoFatal"" TEXT NOT NULL,
            ""Cumplido"" INTEGER NOT NULL,
            ""FechaCumplimiento"" TEXT NULL,
            CONSTRAINT ""FK_PlazosLegales_Expedientes_ExpedienteId"" FOREIGN KEY (""ExpedienteId"") REFERENCES ""Expedientes"" (""Id"") ON DELETE CASCADE
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ParametrosFiscales"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ParametrosFiscales"" PRIMARY KEY AUTOINCREMENT,
            ""Clave"" TEXT NOT NULL,
            ""Nombre"" TEXT NOT NULL,
            ""Valor"" TEXT NOT NULL,
            ""TipoDato"" TEXT NOT NULL,
            ""Categoria"" TEXT NOT NULL,
            ""Descripcion"" TEXT NULL,
            ""Activo"" INTEGER NOT NULL
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""TarifasArancelarias"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_TarifasArancelarias"" PRIMARY KEY AUTOINCREMENT,
            ""Concepto"" TEXT NOT NULL,
            ""EtapaProcesal"" TEXT NOT NULL,
            ""HonorariosHNL"" TEXT NOT NULL,
            ""HonorariosUSD"" TEXT NOT NULL,
            ""GastosOficialesHNL"" TEXT NOT NULL,
            ""GastosOficialesUSD"" TEXT NOT NULL,
            ""AplicaISV"" INTEGER NOT NULL,
            ""Descripcion"" TEXT NULL,
            ""Activo"" INTEGER NOT NULL
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Facturas"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Facturas"" PRIMARY KEY,
            ""NumeroFactura"" TEXT NOT NULL,
            ""NumeroCAI"" TEXT NULL,
            ""ClienteId"" TEXT NOT NULL,
            ""ExpedienteId"" TEXT NULL,
            ""FechaEmision"" TEXT NOT NULL,
            ""FechaVencimiento"" TEXT NOT NULL,
            ""Moneda"" TEXT NOT NULL,
            ""TasaCambio"" TEXT NOT NULL,
            ""SubtotalHonorarios"" TEXT NOT NULL,
            ""SubtotalGastosOficiales"" TEXT NOT NULL,
            ""MontoISV"" TEXT NOT NULL,
            ""TotalFactura"" TEXT NOT NULL,
            ""MontoPagado"" TEXT NOT NULL,
            ""SaldoPendiente"" TEXT NOT NULL,
            ""Estado"" INTEGER NOT NULL,
            ""SolicitadoPor"" TEXT NULL,
            ""AprobadoPor"" TEXT NULL,
            ""FechaAprobacion"" TEXT NULL,
            ""MotivoRechazo"" TEXT NULL,
            ""FechaPago"" TEXT NULL,
            ""MetodoPago"" TEXT NULL,
            ""ReferenciaBancaria"" TEXT NULL,
            ""Observaciones"" TEXT NULL,
            ""HitoProcesal"" TEXT NULL,
            ""CreadoEn"" TEXT NOT NULL,
            CONSTRAINT ""FK_Facturas_Clientes_ClienteId"" FOREIGN KEY (""ClienteId"") REFERENCES ""Clientes"" (""Id"") ON DELETE RESTRICT,
            CONSTRAINT ""FK_Facturas_Expedientes_ExpedienteId"" FOREIGN KEY (""ExpedienteId"") REFERENCES ""Expedientes"" (""Id"") ON DELETE SET NULL
        );
    ");

    // Migraciones en vivo para bases de datos SQLite preexistentes
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Facturas"" ADD COLUMN ""SolicitadoPor"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Facturas"" ADD COLUMN ""AprobadoPor"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Facturas"" ADD COLUMN ""FechaAprobacion"" TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Facturas"" ADD COLUMN ""MotivoRechazo"" TEXT NULL;"); } catch { }

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DetallesFactura"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_DetallesFactura"" PRIMARY KEY,
            ""FacturaId"" TEXT NOT NULL,
            ""TarifaArancelariaId"" INTEGER NULL,
            ""Concepto"" TEXT NOT NULL,
            ""EsGastoOficial"" INTEGER NOT NULL,
            ""Cantidad"" INTEGER NOT NULL,
            ""PrecioUnitario"" TEXT NOT NULL,
            ""Subtotal"" TEXT NOT NULL,
            ""MontoISV"" TEXT NOT NULL,
            ""TotalLinea"" TEXT NOT NULL,
            CONSTRAINT ""FK_DetallesFactura_Facturas_FacturaId"" FOREIGN KEY (""FacturaId"") REFERENCES ""Facturas"" (""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_DetallesFactura_TarifasArancelarias_TarifaArancelariaId"" FOREIGN KEY (""TarifaArancelariaId"") REFERENCES ""TarifasArancelarias"" (""Id"") ON DELETE SET NULL
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Configuraciones"" (
            ""IdConfiguracion"" INTEGER NOT NULL CONSTRAINT ""PK_Configuraciones"" PRIMARY KEY AUTOINCREMENT,
            ""Llave"" TEXT NOT NULL,
            ""Valor"" TEXT NOT NULL,
            ""Descripcion"" TEXT NULL,
            ""Categoria"" TEXT NULL,
            ""EsSensible"" INTEGER NOT NULL,
            ""ActualizadoEn"" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Configuraciones_Llave"" ON ""Configuraciones"" (""Llave"");
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DocumentosExpediente"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_DocumentosExpediente"" PRIMARY KEY,
            ""ExpedienteId"" TEXT NOT NULL,
            ""Categoria"" TEXT NOT NULL,
            ""TipoDocumento"" TEXT NOT NULL,
            ""NombreOriginal"" TEXT NOT NULL,
            ""RutaAlmacenamiento"" TEXT NOT NULL,
            ""Extension"" TEXT NOT NULL,
            ""TipoMime"" TEXT NOT NULL,
            ""TamanoBytes"" INTEGER NOT NULL,
            ""Observaciones"" TEXT NULL,
            ""ProveedorAlmacenamiento"" TEXT NOT NULL,
            ""CustodiaFisicaVerificada"" INTEGER NOT NULL,
            ""UbicacionFisica"" TEXT NULL,
            ""FechaSubida"" TEXT NOT NULL,
            ""SubidoPor"" TEXT NOT NULL,
            CONSTRAINT ""FK_DocumentosExpediente_Expedientes_ExpedienteId"" FOREIGN KEY (""ExpedienteId"") REFERENCES ""Expedientes"" (""Id"") ON DELETE CASCADE
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Usuarios"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Usuarios"" PRIMARY KEY,
            ""NombreCompleto"" TEXT NOT NULL,
            ""Email"" TEXT NOT NULL,
            ""Username"" TEXT NOT NULL,
            ""PasswordHash"" TEXT NOT NULL,
            ""Salt"" TEXT NOT NULL,
            ""Rol"" TEXT NOT NULL,
            ""Cargo"" TEXT NULL,
            ""Telefono"" TEXT NULL,
            ""Activo"" INTEGER NOT NULL,
            ""IntentosFallidos"" INTEGER NOT NULL,
            ""BloqueadoHasta"" TEXT NULL,
            ""UltimoAcceso"" TEXT NULL,
            ""CreadoEn"" TEXT NOT NULL,
            ""ActualizadoEn"" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Usuarios_Email"" ON ""Usuarios"" (""Email"");
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Usuarios_Username"" ON ""Usuarios"" (""Username"");
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""AuditoriasAcceso"" (
            ""Id"" TEXT NOT NULL CONSTRAINT ""PK_AuditoriasAcceso"" PRIMARY KEY,
            ""UsuarioId"" TEXT NULL,
            ""EmailIngresado"" TEXT NOT NULL,
            ""Fecha"" TEXT NOT NULL,
            ""Exitoso"" INTEGER NOT NULL,
            ""IpDireccion"" TEXT NULL,
            ""Navegador"" TEXT NULL,
            ""Detalle"" TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ""IX_AuditoriasAcceso_Fecha"" ON ""AuditoriasAcceso"" (""Fecha"");
    ");

    // Si la tabla de feriados está vacía, poblar los feriados oficiales hondureños
    if (!db.FeriadosNacionales.Any())
    {
        db.FeriadosNacionales.AddRange(
            new FeriadoNacional { Fecha = new DateTime(2026, 1, 1), Nombre = "Año Nuevo", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 4, 14), Nombre = "Día de las Américas", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 5, 1), Nombre = "Día Internacional del Trabajo", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 9, 15), Nombre = "Día de la Independencia Nacional", FundamentoLegal = "Fiesta Cívica Nacional", EsFijoAnual = true, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 12, 25), Nombre = "Navidad", FundamentoLegal = "Art. 339 Código del Trabajo", EsFijoAnual = true, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2025, 4, 17), Nombre = "Jueves Santo 2025", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2025, 4, 18), Nombre = "Viernes Santo 2025", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2025, 10, 1), Nombre = "Feriado Morazánico 2025 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2025, 10, 2), Nombre = "Feriado Morazánico 2025 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2025, 10, 3), Nombre = "Feriado Morazánico 2025 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 4, 2), Nombre = "Jueves Santo 2026", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 4, 3), Nombre = "Viernes Santo 2026", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 10, 7), Nombre = "Feriado Morazánico 2026 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 10, 8), Nombre = "Feriado Morazánico 2026 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2026, 10, 9), Nombre = "Feriado Morazánico 2026 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2027, 3, 25), Nombre = "Jueves Santo 2027", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2027, 3, 26), Nombre = "Viernes Santo 2027", FundamentoLegal = "Semana Santa Oficial", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2027, 10, 6), Nombre = "Feriado Morazánico 2027 (Miércoles)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2027, 10, 7), Nombre = "Feriado Morazánico 2027 (Jueves)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true },
            new FeriadoNacional { Fecha = new DateTime(2027, 10, 8), Nombre = "Feriado Morazánico 2027 (Viernes)", FundamentoLegal = "Decreto Leg. 78-2015", EsFijoAnual = false, Activo = true }
        );
        db.SaveChanges();
    }

    // Si la tabla de Clientes está vacía, poblar el padrón inicial de titulares en la tabla de base de datos
    if (!db.Clientes.Any())
    {
        db.Clientes.AddRange(
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "INDUSTRIAS TEXTILES DE CHOLOMA S.A.",
                NumeroIdentificacionRTN = "05011995887766",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "legal@textilescholoma.hn",
                Telefono = "+504 2669-4500",
                DireccionExacta = "ZIP Choloma, Nave 14, Boulevard a Puerto Cortés, Choloma, Cortés, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            },
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "ROATAN HOSPITALITY GROUP S.A.",
                NumeroIdentificacionRTN = "11012010445566",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "info@roatanhospitality.com",
                Telefono = "+504 2455-7800",
                DireccionExacta = "West Bay Beach, Calle Principal Edificio Turquoise, Roatán, Islas de la Bahía, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            },
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "COOPERATIVA GANADERA DEL SUR LIMITADA",
                NumeroIdentificacionRTN = "06011988332211",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "coop@ganaderadelsur.hn",
                Telefono = "+504 2782-1200",
                DireccionExacta = "Carretera Panamericana Km 5, Salida a San Marcos de Colón, Choluteca, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            },
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "INVERSIONES COPAN S. DE R.L.",
                NumeroIdentificacionRTN = "08011990123456",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "legal@inversionescopan.hn",
                Telefono = "+504 2239-1122",
                DireccionExacta = "Col. Palmira, Ave. República de Chile, Edificio Copán 4to Piso, Tegucigalpa, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            },
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "CERVECERIA DEL VALLE S.A.",
                NumeroIdentificacionRTN = "05011985654321",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "propiedad.intelectual@valle.hn",
                Telefono = "+504 2552-3344",
                DireccionExacta = "Boulevard del Norte, Parque Industrial El Valle, San Pedro Sula, Cortés, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            },
            new Cliente
            {
                Id = Guid.NewGuid(),
                NombreRazonSocial = "DISTRIBUIDORA FARMACEUTICA DEL CARIBE S.A.",
                NumeroIdentificacionRTN = "05011999881122",
                TipoPersona = TipoPersona.JuridicaNacional,
                CorreoElectronico = "asuntos.regulatorios@farmaciascaribe.hn",
                Telefono = "+504 2557-9000",
                DireccionExacta = "Barrio Guamilito, 8 Calle 5 Ave NO, San Pedro Sula, Cortés, Honduras",
                Nacionalidad = "Hondureña",
                CreadoEn = DateTime.UtcNow
            }
        );
        db.SaveChanges();
    }

    // Garantizar que la tabla ParametrosFiscales contenga todos los parámetros base y de facturación SAR
    var parametrosFiscalesBase = new List<ParametroFiscal>
    {
        new() { Clave = "ISV_PORCENTAJE", Nombre = "Porcentaje Impuesto sobre Ventas (ISV)", Valor = "15.00", TipoDato = "decimal", Categoria = "Impuestos", Descripcion = "Tasa general del 15% sobre honorarios profesionales de servicios legales", Activo = true },
        new() { Clave = "TASA_CAMBIO_USD_HNL", Nombre = "Tasa de Cambio Oficial (USD a HNL)", Valor = "25.50", TipoDato = "decimal", Categoria = "Divisas", Descripcion = "Tipo de cambio de referencia del Banco Central de Honduras (BCH)", Activo = true },
        new() { Clave = "TIMBRE_CONTRATACION_HNL", Nombre = "Timbre de Contratación (Colegio de Abogados)", Valor = "50.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Timbre obligatorio del CAH por cada solicitud presentada en ventanilla", Activo = true },
        new() { Clave = "TASA_SOLICITUD_DIGEPIH_HNL", Nombre = "Tasa Oficial de Presentación DIGEPIH", Valor = "700.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Tasa gubernamental de radicación de solicitud por cada clase Niza", Activo = true },
        new() { Clave = "TASA_REGISTRO_TITULO_HNL", Nombre = "Tasa Oficial de Emisión de Certificado", Valor = "500.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Tasa oficial ante DIGEPIH por emisión del título de concesión", Activo = true },
        new() { Clave = "TASA_PUBLICACION_ENAG_HNL", Nombre = "Tasa de Publicación en La Gaceta (ENAG)", Valor = "650.00", TipoDato = "decimal", Categoria = "Tasas Oficiales", Descripcion = "Pago a la Empresa Nacional de Artes Gráficas por los 3 avisos de ley", Activo = true },
        new() { Clave = "CAI_SAR_AUTORIZADO", Nombre = "Código de Autorización de Impresión (CAI)", Valor = "A84F2B-98CE21-1B4480-1498B2-FA8321-44", TipoDato = "string", Categoria = "Facturación SAR", Descripcion = "Régimen de facturación autorizado por el Servicio de Administración de Rentas", Activo = true },
        new() { Clave = "FECHA_LIMITE_SAR", Nombre = "Fecha Límite de Emisión de Facturas", Valor = "2027-12-31", TipoDato = "date", Categoria = "Facturación SAR", Descripcion = "Fecha máxima de vigencia del rango de facturación asignado por SAR", Activo = true },
        new() { Clave = "RTN_EMISOR", Nombre = "RTN del Emisor (Bufete)", Valor = "08011990123456", TipoDato = "string", Categoria = "Datos del Emisor", Descripcion = "Registro Tributario Nacional del emisor autorizado ante el SAR", Activo = true },
        new() { Clave = "NOMBRE_EMISOR", Nombre = "Razón Social del Emisor", Valor = "LegalTech Honduras S. de R.L.", TipoDato = "string", Categoria = "Datos del Emisor", Descripcion = "Nombre comercial o razón social registrada en el SAR", Activo = true },
        new() { Clave = "DIRECCION_EMISOR", Nombre = "Dirección Fiscal del Emisor", Valor = "Edificio Corporativo Centro Morazán, Torre 1, Piso 7, Boulevard Morazán, Tegucigalpa, M.D.C., Francisco Morazán, Honduras", TipoDato = "string", Categoria = "Datos del Emisor", Descripcion = "Dirección fiscal del establecimiento matriz", Activo = true },
        new() { Clave = "TELEFONO_EMISOR", Nombre = "Teléfono de Contacto", Valor = "+504 2239-0000 / +504 9988-7766", TipoDato = "string", Categoria = "Datos del Emisor", Descripcion = "Teléfonos del bufete para atención y consultas fiscales", Activo = true },
        new() { Clave = "CORREO_EMISOR", Nombre = "Correo Electrónico de Facturación", Valor = "facturacion@legaltech.hn", TipoDato = "string", Categoria = "Datos del Emisor", Descripcion = "Correo oficial del departamento de cobranzas y facturación", Activo = true },
        new() { Clave = "RANGO_AUTORIZADO_SAR", Nombre = "Rango Autorizado por el SAR", Valor = "001-001-01-00000001 al 001-001-01-00050000", TipoDato = "string", Categoria = "Facturación SAR", Descripcion = "Rango de numeración fiscal autorizada por el SAR", Activo = true }
    };

    foreach (var p in parametrosFiscalesBase)
    {
        if (!db.ParametrosFiscales.Any(x => x.Clave == p.Clave))
        {
            db.ParametrosFiscales.Add(p);
        }
    }
    db.SaveChanges();

    // Normalizar facturas emitidas o registradas previamente que aún tengan el prefijo preliminar FAC-2026-XXXX al formato oficial del SAR
    var facturasLegacy = db.Facturas.Where(f => f.NumeroFactura.StartsWith("FAC-2026-")).ToList();
    if (facturasLegacy.Any())
    {
        foreach (var fac in facturasLegacy)
        {
            var partes = fac.NumeroFactura.Split('-');
            if (partes.Length >= 3 && int.TryParse(partes[2], out int corr))
            {
                fac.NumeroFactura = $"001-001-01-{corr:D8}";
            }
        }
        db.SaveChanges();
    }

    // Inicializar llaves base de configuración (RutaLocal, Synology, etc.)
    var configService = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();
    configService.InicializarConfiguracionesBaseAsync(db).GetAwaiter().GetResult();

    // Inicializar usuarios base del despacho (Socio Director, Abogado, Paralegal, Finanzas)
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    authService.InicializarUsuariosBaseAsync(db).GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Endpoint HTTP para Iniciar Sesión con Cookie Segura
app.MapPost("/account/login", async (
    HttpContext context,
    [FromForm] string identificador,
    [FromForm] string password,
    [FromQuery] string? returnUrl,
    AuthService authService) =>
{
    var resultado = await authService.ValidarCredencialesAsync(
        identificador, password, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent);

    if (!resultado.Exitoso || resultado.Sesion == null)
    {
        var error = Uri.EscapeDataString(resultado.Error ?? "Error de credenciales");
        return Results.Redirect($"/login?error={error}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, resultado.Sesion.Id.ToString()),
        new(ClaimTypes.Name, resultado.Sesion.NombreCompleto),
        new(ClaimTypes.Email, resultado.Sesion.Email),
        new(ClaimTypes.Role, resultado.Sesion.Rol),
        new(ClaimTypes.GivenName, resultado.Sesion.Username),
        new("Cargo", resultado.Sesion.Cargo),
        new("TokenSesion", resultado.Sesion.TokenSesion)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
    });

    string destino = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/") ? returnUrl : "/";
    return Results.Redirect(destino);
}).DisableAntiforgery();

// Endpoint HTTP para Cerrar Sesión Segura
app.MapPost("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

// Endpoint seguro para streaming de documentos confidenciales (PDF, imágenes, escritos)
app.MapGet("/api/documentos/{id:guid}", async (Guid id, IDbContextFactory<LegalTechDbContext> factory, IWebHostEnvironment env, HttpContext context) =>
{
    using var db = await factory.CreateDbContextAsync();
    var doc = await db.DocumentosExpediente.FindAsync(id);
    if (doc == null)
    {
        return Results.NotFound("Documento confidencial no encontrado.");
    }

    if (doc.RutaAlmacenamiento.StartsWith("/"))
    {
        string webRoot = env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        string rutaFisica = Path.Combine(webRoot, doc.RutaAlmacenamiento.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(rutaFisica))
        {
            var stream = File.OpenRead(rutaFisica);
            bool esDescarga = context.Request.Query.ContainsKey("download");
            return Results.File(stream, doc.TipoMime, esDescarga ? doc.NombreOriginal : null, enableRangeProcessing: true);
        }
    }

    return Results.NotFound("El archivo físico no fue localizado en el servidor.");
});

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
