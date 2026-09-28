using LegalTech.Web.Components;
using LegalTech.Web.Domain.Entities;
using LegalTech.Web.Domain.Enums;
using LegalTech.Web.Infrastructure.Data;
using LegalTech.Web.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

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
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
