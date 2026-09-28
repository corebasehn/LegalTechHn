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
/// Servicio integral de Facturación, Tarifarios Arancelarios, Parámetros Fiscales
/// y Cumplimiento de la Regla Infranqueable de Solvencia P360 (Manual de Marcas de Honduras).
/// </summary>
public class FacturacionService
{
    private readonly IDbContextFactory<LegalTechDbContext> _factory;

    public FacturacionService(IDbContextFactory<LegalTechDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // 1. GESTIÓN DE PARÁMETROS FISCALES Y REGULATORIOS (DINÁMICOS EN BD)
    // =========================================================================

    public async Task<List<ParametroFiscal>> GetParametrosFiscalesAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        await InicializarParametrosBaseAsync(db);
        return await db.ParametrosFiscales
            .OrderBy(p => p.Categoria)
            .ThenBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<ParametroFiscal> GuardarParametroFiscalAsync(ParametroFiscal parametro)
    {
        using var db = await _factory.CreateDbContextAsync();
        if (parametro.Id == 0)
        {
            db.ParametrosFiscales.Add(parametro);
        }
        else
        {
            var existente = await db.ParametrosFiscales.FindAsync(parametro.Id)
                ?? throw new InvalidOperationException("Parámetro fiscal no encontrado.");

            existente.Clave = parametro.Clave;
            existente.Nombre = parametro.Nombre;
            existente.Valor = parametro.Valor;
            existente.TipoDato = parametro.TipoDato;
            existente.Categoria = parametro.Categoria;
            existente.Descripcion = parametro.Descripcion;
            existente.Activo = parametro.Activo;
        }

        await db.SaveChangesAsync();
        return parametro;
    }

    public async Task ToggleParametroFiscalAsync(int id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var p = await db.ParametrosFiscales.FindAsync(id);
        if (p != null)
        {
            p.Activo = !p.Activo;
            await db.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetParametroDecimalAsync(string clave, decimal valorDefecto = 0m)
    {
        using var db = await _factory.CreateDbContextAsync();
        var p = await db.ParametrosFiscales.FirstOrDefaultAsync(x => x.Clave == clave && x.Activo);
        if (p != null && decimal.TryParse(p.Valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
        {
            return val;
        }
        return valorDefecto;
    }

    public async Task<string> GetParametroStringAsync(string clave, string valorDefecto = "")
    {
        using var db = await _factory.CreateDbContextAsync();
        var p = await db.ParametrosFiscales.FirstOrDefaultAsync(x => x.Clave == clave && x.Activo);
        return p?.Valor ?? valorDefecto;
    }

    // =========================================================================
    // 2. GESTIÓN DE TARIFAS ARANCELARIAS Y HONORARIOS POR ETAPA PROCESAL
    // =========================================================================

    public async Task<List<TarifaArancelaria>> GetTarifasAsync(bool soloActivas = false)
    {
        using var db = await _factory.CreateDbContextAsync();
        if (!await db.TarifasArancelarias.AnyAsync())
        {
            await InicializarTarifasBaseAsync(db);
        }
        var query = db.TarifasArancelarias.AsQueryable();
        if (soloActivas)
        {
            query = query.Where(t => t.Activo);
        }
        return await query.OrderBy(t => t.EtapaProcesal).ThenBy(t => t.Concepto).ToListAsync();
    }

    public async Task<TarifaArancelaria> GuardarTarifaAsync(TarifaArancelaria tarifa)
    {
        using var db = await _factory.CreateDbContextAsync();
        if (tarifa.Id == 0)
        {
            db.TarifasArancelarias.Add(tarifa);
        }
        else
        {
            var existente = await db.TarifasArancelarias.FindAsync(tarifa.Id)
                ?? throw new InvalidOperationException("Tarifa arancelaria no encontrada.");

            existente.Concepto = tarifa.Concepto;
            existente.EtapaProcesal = tarifa.EtapaProcesal;
            existente.HonorariosHNL = tarifa.HonorariosHNL;
            existente.HonorariosUSD = tarifa.HonorariosUSD;
            existente.GastosOficialesHNL = tarifa.GastosOficialesHNL;
            existente.GastosOficialesUSD = tarifa.GastosOficialesUSD;
            existente.AplicaISV = tarifa.AplicaISV;
            existente.Descripcion = tarifa.Descripcion;
            existente.Activo = tarifa.Activo;
        }

        await db.SaveChangesAsync();
        return tarifa;
    }

    public async Task ToggleTarifaAsync(int id)
    {
        using var db = await _factory.CreateDbContextAsync();
        var tarifa = await db.TarifasArancelarias.FindAsync(id);
        if (tarifa != null)
        {
            tarifa.Activo = !tarifa.Activo;
            await db.SaveChangesAsync();
        }
    }

    // =========================================================================
    // 3. FACTURACIÓN Y COBRANZAS DE EXPEDIENTES
    // =========================================================================

    public async Task<List<FacturaCobro>> GetFacturasAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        if (await db.Facturas.CountAsync() < 4)
        {
            await InicializarDatosDemostrativosFacturacionAsync(db);
        }

        return await db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Expediente)
            .Include(f => f.Detalles)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();
    }

    public async Task<List<FacturaCobro>> GetFacturasPorExpedienteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas
            .Where(f => f.ExpedienteId == expedienteId)
            .Include(f => f.Cliente)
            .Include(f => f.Detalles)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();
    }

    public async Task<List<FacturaCobro>> GetFacturasPorClienteAsync(Guid clienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas
            .Where(f => f.ClienteId == clienteId)
            .Include(f => f.Expediente)
            .Include(f => f.Detalles)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();
    }

    public async Task<FacturaCobro?> GetFacturaByIdAsync(Guid id)
    {
        using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Expediente)
            .Include(f => f.Detalles)
                .ThenInclude(d => d.TarifaArancelaria)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    /// <summary>
    /// Guarda una factura recalculando totales, montos de ISV (15% sobre honorarios) y saldos pendientes.
    /// </summary>
    public async Task<FacturaCobro> GuardarFacturaAsync(FacturaCobro factura, string usuario = "Usuario")
    {
        using var db = await _factory.CreateDbContextAsync();

        decimal tasaISV = await GetParametroDecimalAsync("ISV_PORCENTAJE", 15.00m) / 100m;
        string caiDefecto = await GetParametroStringAsync("CAI_SAR_AUTORIZADO", "A84F2B-98CE21-1B4480-1498B2-FA8321-44");

        if (string.IsNullOrWhiteSpace(factura.NumeroCAI))
        {
            factura.NumeroCAI = caiDefecto;
        }

        // Recalcular líneas y subtotales
        decimal honorarios = 0m;
        decimal gastosOficiales = 0m;
        decimal totalISV = 0m;

        foreach (var det in factura.Detalles)
        {
            det.Subtotal = det.Cantidad * det.PrecioUnitario;
            if (det.EsGastoOficial)
            {
                det.MontoISV = 0m;
                gastosOficiales += det.Subtotal;
            }
            else
            {
                det.MontoISV = Math.Round(det.Subtotal * tasaISV, 2);
                honorarios += det.Subtotal;
                totalISV += det.MontoISV;
            }
            det.TotalLinea = det.Subtotal + det.MontoISV;
        }

        factura.SubtotalHonorarios = honorarios;
        factura.SubtotalGastosOficiales = gastosOficiales;
        factura.MontoISV = totalISV;
        factura.TotalFactura = honorarios + gastosOficiales + totalISV;
        factura.SaldoPendiente = Math.Max(0m, factura.TotalFactura - factura.MontoPagado);

        // Actualizar estado de cobro según saldo si no es borrador o proforma
        if (factura.Estado != EstadoFactura.Borrador && 
            factura.Estado != EstadoFactura.PendienteAprobacion && 
            factura.Estado != EstadoFactura.Rechazada && 
            factura.Estado != EstadoFactura.Anulada)
        {
            if (factura.MontoPagado >= factura.TotalFactura && factura.TotalFactura > 0)
                factura.Estado = EstadoFactura.Pagada;
            else if (factura.MontoPagado > 0)
                factura.Estado = EstadoFactura.Parcial;
            else
                factura.Estado = EstadoFactura.Emitida;
        }

        if (factura.Estado == EstadoFactura.Borrador || factura.Estado == EstadoFactura.PendienteAprobacion)
        {
            factura.SolicitadoPor ??= usuario;
        }
        else if (factura.Estado == EstadoFactura.Emitida)
        {
            factura.AprobadoPor ??= usuario;
            factura.FechaAprobacion ??= DateTime.UtcNow;
        }

        if (factura.Id == Guid.Empty)
        {
            factura.Id = Guid.NewGuid();
            factura.CreadoEn = DateTime.UtcNow;
            db.Facturas.Add(factura);
        }
        else
        {
            var existente = await db.Facturas.Include(f => f.Detalles).FirstOrDefaultAsync(f => f.Id == factura.Id);
            if (existente != null)
            {
                db.Entry(existente).CurrentValues.SetValues(factura);

                // Reemplazar detalles
                db.DetallesFactura.RemoveRange(existente.Detalles);
                foreach (var d in factura.Detalles)
                {
                    d.FacturaId = factura.Id;
                    if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                    db.DetallesFactura.Add(d);
                }
            }
            else
            {
                db.Facturas.Add(factura);
            }
        }

        await db.SaveChangesAsync();

        // Validar impacto en la Regla de Solvencia P360 si hay expediente vinculado
        if (factura.ExpedienteId.HasValue)
        {
            await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuario);
        }

        return factura;
    }

    /// <summary>
    /// Envía una factura proforma/borrador a revisión y aprobación por la dirección o socio legal.
    /// </summary>
    public async Task EnviarAAprobacionAsync(Guid facturaId, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        factura.Estado = EstadoFactura.PendienteAprobacion;
        factura.SolicitadoPor = usuario;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Aprueba formalmente una proforma/borrador, asigna correlativo oficial SAR, CAI y activa la compuerta P360.
    /// </summary>
    public async Task<FacturaCobro> AprobarFacturaAsync(Guid facturaId, string usuarioAprobador)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        factura.Estado = EstadoFactura.Emitida;
        factura.AprobadoPor = usuarioAprobador;
        factura.FechaAprobacion = DateTime.UtcNow;
        factura.MotivoRechazo = null;

        // Si era una proforma con prefijo PROF- o BORR-, asignarle un correlativo oficial FAC-
        if (factura.NumeroFactura.StartsWith("PROF-") || factura.NumeroFactura.StartsWith("BORR-"))
        {
            int correlativo = await db.Facturas.CountAsync(f => f.Estado != EstadoFactura.Borrador && f.Estado != EstadoFactura.PendienteAprobacion && f.Estado != EstadoFactura.Rechazada) + 1;
            factura.NumeroFactura = $"FAC-2026-{correlativo:D4}";
        }

        // Si no tenía CAI oficial, asignarle el CAI autorizado
        if (string.IsNullOrEmpty(factura.NumeroCAI))
        {
            var pCai = await db.ParametrosFiscales.FirstOrDefaultAsync(p => p.Clave == "CAI_SAR_AUTORIZADO" && p.Activo);
            factura.NumeroCAI = pCai?.Valor ?? "A84F2B-98CE21-1B4480-1498B2-FA8321-44";
        }

        await db.SaveChangesAsync();

        if (factura.ExpedienteId.HasValue)
        {
            await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuarioAprobador);
        }

        return factura;
    }

    /// <summary>
    /// Rechaza una factura o proforma con un motivo explicativo.
    /// </summary>
    public async Task RechazarFacturaAsync(Guid facturaId, string motivo, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        factura.Estado = EstadoFactura.Rechazada;
        factura.MotivoRechazo = $"[Rechazado el {DateTime.UtcNow:dd/MM/yyyy HH:mm} por {usuario}]: {motivo}";
        await db.SaveChangesAsync();

        if (factura.ExpedienteId.HasValue)
        {
            await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuario);
        }
    }

    /// <summary>
    /// Devuelve una factura a estado Borrador para correcciones de conceptos, honorarios o gastos oficiales.
    /// </summary>
    public async Task DevolverABorradorAsync(Guid facturaId, string motivo, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        factura.Estado = EstadoFactura.Borrador;
        factura.MotivoRechazo = $"[Devuelto a borrador el {DateTime.UtcNow:dd/MM/yyyy HH:mm} por {usuario}]: {motivo}";
        await db.SaveChangesAsync();

        if (factura.ExpedienteId.HasValue)
        {
            await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuario);
        }
    }

    /// <summary>
    /// Registra un abono o pago completo a una factura, y si la deuda del expediente llega a cero,
    /// acredita automáticamente la Solvencia P360 desbloqueando el despacho del título original.
    /// </summary>
    public async Task RegistrarPagoAsync(Guid facturaId, decimal montoAbono, string metodoPago, string? referencia, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId)
            ?? throw new InvalidOperationException("Factura no encontrada.");

        factura.MontoPagado += montoAbono;
        factura.SaldoPendiente = Math.Max(0m, factura.TotalFactura - factura.MontoPagado);
        factura.FechaPago = DateTime.UtcNow;
        factura.MetodoPago = metodoPago;
        factura.ReferenciaBancaria = referencia;

        if (factura.SaldoPendiente == 0)
        {
            factura.Estado = EstadoFactura.Pagada;
        }
        else
        {
            factura.Estado = EstadoFactura.Parcial;
        }

        await db.SaveChangesAsync();

        // Verificar y sincronizar solvencia P360
        if (factura.ExpedienteId.HasValue)
        {
            await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuario);
        }
    }

    public async Task AnularFacturaAsync(Guid facturaId, string motivo, string usuario)
    {
        using var db = await _factory.CreateDbContextAsync();
        var factura = await db.Facturas.FindAsync(facturaId);
        if (factura != null)
        {
            factura.Estado = EstadoFactura.Anulada;
            factura.Observaciones = $"[ANULADA el {DateTime.UtcNow:dd/MM/yyyy} por {usuario}]: {motivo}\n{factura.Observaciones}".Trim();
            await db.SaveChangesAsync();

            if (factura.ExpedienteId.HasValue)
            {
                await SincronizarSolvenciaP360InternoAsync(db, factura.ExpedienteId.Value, usuario);
            }
        }
    }

    // =========================================================================
    // 4. MOTOR DE LA REGLA INFRANQUEABLE DE SOLVENCIA P360
    // =========================================================================

    /// <summary>
    /// Consulta el estado de solvencia de un expediente (si tiene facturas con saldo pendiente).
    /// Solo las facturas formalmente emitidas o con cobros parciales generan deuda exigible para P360.
    /// Las proformas en borrador o pendientes de aprobación no bloquean la solvencia.
    /// </summary>
    public async Task<(bool EsSolvente, decimal SaldoPendienteTotal, int CantidadFacturasPendientes)> ConsultarSolvenciaExpedienteAsync(Guid expedienteId)
    {
        using var db = await _factory.CreateDbContextAsync();
        var facturas = await db.Facturas
            .Where(f => f.ExpedienteId == expedienteId && 
                       (f.Estado == EstadoFactura.Emitida || f.Estado == EstadoFactura.Parcial))
            .ToListAsync();

        decimal saldoTotal = facturas.Sum(f => f.SaldoPendiente);
        int pendientes = facturas.Count(f => f.SaldoPendiente > 0);

        return (saldoTotal == 0, saldoTotal, pendientes);
    }

    /// <summary>
    /// Sincroniza internamente el estado de solvencia en la tabla Certificados.
    /// </summary>
    private async Task SincronizarSolvenciaP360InternoAsync(LegalTechDbContext db, Guid expedienteId, string usuario)
    {
        var facturas = await db.Facturas
            .Where(f => f.ExpedienteId == expedienteId && 
                       (f.Estado == EstadoFactura.Emitida || f.Estado == EstadoFactura.Parcial))
            .ToListAsync();

        decimal saldoTotal = facturas.Sum(f => f.SaldoPendiente);
        bool esSolvente = saldoTotal == 0;

        var certificado = await db.Certificados.FirstOrDefaultAsync(c => c.ExpedienteId == expedienteId);
        if (certificado == null)
        {
            certificado = new CertificadoRegistro
            {
                Id = Guid.NewGuid(),
                ExpedienteId = expedienteId,
                FechaConcesion = DateTime.UtcNow,
                FechaVencimientoDecenal = DateTime.UtcNow.AddYears(10),
                UbicacionArchivoId = 3, // Bóveda de Seguridad
                SolvenciaValidada = false
            };
            db.Certificados.Add(certificado);
        }

        if (esSolvente && !certificado.SolvenciaValidada)
        {
            certificado.SolvenciaValidada = true;
            certificado.FechaValidacionSolvencia = DateTime.UtcNow;
            certificado.ValidadoPorUsuario = $"{usuario} (Sistema de Cobranzas P360)";

            // Registrar actuación en la línea de tiempo del expediente
            var actuacion = new ActuacionExpediente
            {
                ExpedienteId = expedienteId,
                TipoActuacionId = 8, // Concesión / Notificación
                Titulo = "Solvencia P360 Acreditada - Título Habilitado para Despacho",
                Comentario = "Se verificó saldo contable en L. 0.00. Se desbloqueó la compuerta P360 para entrega formal del Certificado de Registro original al titular.",
                FechaActuacion = DateTime.UtcNow,
                UsuarioResponsable = usuario
            };
            db.ActuacionesExpediente.Add(actuacion);
        }
        else if (!esSolvente && certificado.SolvenciaValidada)
        {
            // Si se generó una nueva factura con saldo, se reactiva el candado de seguridad
            certificado.SolvenciaValidada = false;
            certificado.ValidadoPorUsuario = null;
        }

        await db.SaveChangesAsync();
    }

    public async Task<bool> SincronizarSolvenciaP360Async(Guid expedienteId, string usuario = "Sistema Financiero P360")
    {
        using var db = await _factory.CreateDbContextAsync();
        await SincronizarSolvenciaP360InternoAsync(db, expedienteId, usuario);
        var res = await ConsultarSolvenciaExpedienteAsync(expedienteId);
        return res.EsSolvente;
    }

    // =========================================================================
    // 5. DATOS DEMOSTRATIVOS DE FACTURACIÓN Y ARANCELES
    // =========================================================================

    private async Task InicializarDatosDemostrativosFacturacionAsync(LegalTechDbContext db)
    {
        var expedientes = await db.Expedientes.OrderBy(e => e.CodigoInterno).ToListAsync();
        var clientes = await db.Clientes.OrderBy(c => c.NombreRazonSocial).ToListAsync();

        var expConcedido = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0005") ?? expedientes.FirstOrDefault();
        var expGaceta = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0001") ?? expedientes.FirstOrDefault();
        var expLitigio = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0004") ?? expedientes.FirstOrDefault();
        var expDolares = expedientes.FirstOrDefault(e => e.CodigoInterno == "EXP-2026-0002") ?? expedientes.FirstOrDefault();

        string cai = "A84F2B-98CE21-1B4480-1498B2-FA8321-44";

        var demoFacturas = new List<FacturaCobro>
        {
            // 1. FACTURA TOTALMENTE PAGADA (EXP-2026-0002 VALLE TECH SOFTWARE - SOLVENCIA P360 ACREDITADA)
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "FAC-2026-0001",
                NumeroCAI = cai,
                ClienteId = expConcedido?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expConcedido?.Id,
                FechaEmision = DateTime.Today.AddDays(-40),
                FechaVencimiento = DateTime.Today.AddDays(-25),
                Moneda = "HNL",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 11500.00m,   // 8500 solicitud + 3000 título
                SubtotalGastosOficiales = 1900.00m, // 1400 (DIGEPIH/ENAG/Timbre) + 500 (Título)
                MontoISV = 1725.00m,              // 15% sobre 11500
                TotalFactura = 15125.00m,
                MontoPagado = 15125.00m,
                SaldoPendiente = 0.00m,
                Estado = EstadoFactura.Pagada,
                FechaPago = DateTime.Today.AddDays(-24),
                MetodoPago = "Transferencia Bancaria ACH (BAC Credomatic)",
                ReferenciaBancaria = "BAC-DEP-994821",
                HitoProcesal = "Título y Concesión Final",
                Observaciones = "Pago total acreditado. Solvencia P360 validada satisfactoriamente para retiro del certificado original de bóveda.",
                CreadoEn = DateTime.UtcNow.AddDays(-40),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Solicitud y Trámite Completo de Registro (Clase Niza 42)", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 8500.00m, Subtotal = 8500.00m, MontoISV = 1275.00m, TotalLinea = 9775.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Gastos Oficiales: Tasas DIGEPIH (L 700) + Timbre CAH (L 50) + La Gaceta (L 650)", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 1400.00m, Subtotal = 1400.00m, MontoISV = 0.00m, TotalLinea = 1400.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Concesión, Retiro de Certificado Original y Depósito en Bóveda", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 3000.00m, Subtotal = 3000.00m, MontoISV = 450.00m, TotalLinea = 3450.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Gasto Oficial: Tasa DIGEPIH de Emisión de Título de Registro", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 500.00m, Subtotal = 500.00m, MontoISV = 0.00m, TotalLinea = 500.00m }
                }
            },

            // 2. FACTURA EMITIDA PENDIENTE DE PAGO (EXP-2026-0001 CAFÉ DE LA SIERRA MONTES - BLOQUEADO P360)
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "FAC-2026-0002",
                NumeroCAI = cai,
                ClienteId = expGaceta?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expGaceta?.Id,
                FechaEmision = DateTime.Today.AddDays(-10),
                FechaVencimiento = DateTime.Today.AddDays(5),
                Moneda = "HNL",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 8500.00m,
                SubtotalGastosOficiales = 1400.00m,
                MontoISV = 1275.00m,
                TotalFactura = 11175.00m,
                MontoPagado = 0.00m,
                SaldoPendiente = 11175.00m,
                Estado = EstadoFactura.Emitida,
                HitoProcesal = "Publicación en La Gaceta",
                Observaciones = "Factura emitida correspondiente a la radicación y ciclo de publicaciones ENAG. Saldo pendiente de pago.",
                CreadoEn = DateTime.UtcNow.AddDays(-10),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Honorarios de Presentación y Trámite Marcario (Clase 30)", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 8500.00m, Subtotal = 8500.00m, MontoISV = 1275.00m, TotalLinea = 9775.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Tasas Gubernamentales DIGEPIH, Timbre CAH y La Gaceta", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 1400.00m, Subtotal = 1400.00m, MontoISV = 0.00m, TotalLinea = 1400.00m }
                }
            },

            // 3. FACTURA CON PAGO PARCIAL (EXP-2026-0004 BALEADAS EXPRESS - LITIGIO Y OPOSICIÓN)
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "FAC-2026-0003",
                NumeroCAI = cai,
                ClienteId = expLitigio?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expLitigio?.Id,
                FechaEmision = DateTime.Today.AddDays(-15),
                FechaVencimiento = DateTime.Today.AddDays(15),
                Moneda = "HNL",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 10000.00m,   // Oposición
                SubtotalGastosOficiales = 50.00m,   // Timbre escrito
                MontoISV = 1500.00m,
                TotalFactura = 11550.00m,
                MontoPagado = 6000.00m,
                SaldoPendiente = 5550.00m,
                Estado = EstadoFactura.Parcial,
                FechaPago = DateTime.Today.AddDays(-5),
                MetodoPago = "Cheque No. 44812 (Banco Ficohsa)",
                ReferenciaBancaria = "FIC-CHK-44812",
                HitoProcesal = "Controversia y Litigio",
                Observaciones = "Abono del 50% para inicio de redacción de memorial de contestación de oposición.",
                CreadoEn = DateTime.UtcNow.AddDays(-15),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Defensa Jurídica ante Oposición de Terceros (10 días)", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 10000.00m, Subtotal = 10000.00m, MontoISV = 1500.00m, TotalLinea = 11500.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Timbre del Colegio de Abogados para escrito de oposición", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 50.00m, Subtotal = 50.00m, MontoISV = 0.00m, TotalLinea = 50.00m }
                }
            },

            // 4. FACTURA INTERNACIONAL EN DÓLARES (EXP-2026-0005 ROATAN TURQUOISE RESORT)
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "FAC-2026-0004",
                NumeroCAI = cai,
                ClienteId = expDolares?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expDolares?.Id,
                FechaEmision = DateTime.Today.AddDays(-8),
                FechaVencimiento = DateTime.Today.AddDays(22),
                Moneda = "USD",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 500.00m,      // USD
                SubtotalGastosOficiales = 30.00m,  // USD
                MontoISV = 75.00m,                 // 15% USD
                TotalFactura = 605.00m,
                MontoPagado = 605.00m,
                SaldoPendiente = 0.00m,
                Estado = EstadoFactura.Pagada,
                FechaPago = DateTime.Today.AddDays(-2),
                MetodoPago = "Transferencia Internacional Wire (Chase Bank)",
                ReferenciaBancaria = "WIRE-US-99120",
                HitoProcesal = "Acción de Cancelación por No Uso",
                Observaciones = "Cliente internacional en dólares. Factura pagada y acreditada al 100%.",
                CreadoEn = DateTime.UtcNow.AddDays(-8),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Acción de Cancelación por No Uso (3 años) y Auditoría Digital", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 500.00m, Subtotal = 500.00m, MontoISV = 75.00m, TotalLinea = 575.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Tasa de Expedición de Constancia de Rehabilitación DIGEPIH", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 30.00m, Subtotal = 30.00m, MontoISV = 0.00m, TotalLinea = 30.00m }
                }
            },

            // 5. PROFORMA / NOTA DE COBRO EN BORRADOR (PREPARACIÓN POR PARALEGAL)
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "PROF-2026-0005",
                NumeroCAI = null, // Sin CAI fiscal por ser borrador
                ClienteId = expGaceta?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expGaceta?.Id,
                FechaEmision = DateTime.Today.AddDays(-2),
                FechaVencimiento = DateTime.Today.AddDays(13),
                Moneda = "HNL",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 2500.00m,
                SubtotalGastosOficiales = 0.00m,
                MontoISV = 375.00m, // 15% de 2500
                TotalFactura = 2875.00m,
                MontoPagado = 0.00m,
                SaldoPendiente = 2875.00m,
                Estado = EstadoFactura.Borrador,
                SolicitadoPor = "Lic. Andrea Paz (Paralegal)",
                HitoProcesal = "Apertura y Búsqueda",
                Observaciones = "Cotización preliminar enviada a revisión interna. Honorarios por búsqueda registral profunda.",
                CreadoEn = DateTime.UtcNow.AddDays(-2),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Búsqueda Preliminar de Antecedentes y Dictamen de Viabilidad", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 2500.00m, Subtotal = 2500.00m, MontoISV = 375.00m, TotalLinea = 2875.00m }
                }
            },

            // 6. PROFORMA PENDIENTE DE APROBACIÓN POR SOCIO DIRECTOR / FINANZAS
            new()
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "PROF-2026-0006",
                NumeroCAI = null,
                ClienteId = expLitigio?.ClienteId ?? clientes.First().Id,
                ExpedienteId = expLitigio?.Id,
                FechaEmision = DateTime.Today.AddDays(-1),
                FechaVencimiento = DateTime.Today.AddDays(14),
                Moneda = "HNL",
                TasaCambio = 25.50m,
                SubtotalHonorarios = 7000.00m,
                SubtotalGastosOficiales = 50.00m,
                MontoISV = 1050.00m, // 15% de 7000
                TotalFactura = 8100.00m,
                MontoPagado = 0.00m,
                SaldoPendiente = 8100.00m,
                Estado = EstadoFactura.PendienteAprobacion,
                SolicitadoPor = "Abg. Marco Tulio Valle",
                HitoProcesal = "Controversia y Litigio",
                Observaciones = "Propuesta de honorarios para contestar objeción formal emitida por DIGEPIH. Pendiente de visto bueno de Dirección Financiera.",
                CreadoEn = DateTime.UtcNow.AddDays(-1),
                Detalles = new List<DetalleFacturaCobro>
                {
                    new() { Id = Guid.NewGuid(), Concepto = "Contestación de Objeción de Fondo (60 días hábiles)", EsGastoOficial = false, Cantidad = 1, PrecioUnitario = 7000.00m, Subtotal = 7000.00m, MontoISV = 1050.00m, TotalLinea = 8050.00m },
                    new() { Id = Guid.NewGuid(), Concepto = "Timbre CAH de presentación de escrito de fondo", EsGastoOficial = true, Cantidad = 1, PrecioUnitario = 50.00m, Subtotal = 50.00m, MontoISV = 0.00m, TotalLinea = 50.00m }
                }
            }
        };

        db.Facturas.AddRange(demoFacturas);
        await db.SaveChangesAsync();
    }

    private async Task InicializarParametrosBaseAsync(LegalTechDbContext db)
    {
        var parametrosBase = new List<ParametroFiscal>
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

        foreach (var p in parametrosBase)
        {
            if (!await db.ParametrosFiscales.AnyAsync(x => x.Clave == p.Clave))
            {
                db.ParametrosFiscales.Add(p);
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Convierte un monto numérico a su representación en letras en español según exigencia del SAR.
    /// </summary>
    public static string ConvertirNumeroALetras(decimal numero, string moneda = "HNL")
    {
        if (numero < 0) numero = Math.Abs(numero);
        long entero = (long)Math.Truncate(numero);
        int centavos = (int)Math.Round((numero - entero) * 100m);

        string nombreMoneda = moneda.ToUpperInvariant() switch
        {
            "USD" => entero == 1 ? "DÓLAR" : "DÓLARES",
            _ => entero == 1 ? "LEMPIRA" : "LEMPIRAS"
        };

        string textoEntero = entero == 0 ? "CERO" : ConvertirEnteroALetras(entero);
        return $"SON: {textoEntero} {nombreMoneda} CON {centavos:D2}/100";
    }

    private static string ConvertirEnteroALetras(long value)
    {
        if (value == 0) return "CERO";
        if (value == 100) return "CIEN";
        if (value < 0) return "MENOS " + ConvertirEnteroALetras(-value);

        if (value < 10)
        {
            return value switch
            {
                1 => "UN",
                2 => "DOS",
                3 => "TRES",
                4 => "CUATRO",
                5 => "CINCO",
                6 => "SEIS",
                7 => "SIETE",
                8 => "OCHO",
                9 => "NUEVE",
                _ => ""
            };
        }

        if (value <= 15)
        {
            return value switch
            {
                10 => "DIEZ",
                11 => "ONCE",
                12 => "DOCE",
                13 => "TRECE",
                14 => "CATORCE",
                15 => "QUINCE",
                _ => ""
            };
        }

        if (value < 20) return "DIECI" + ConvertirEnteroALetras(value - 10);
        if (value == 20) return "VEINTE";
        if (value < 30) return "VEINTI" + ConvertirEnteroALetras(value - 20);

        if (value < 100)
        {
            string decena = (value / 10) switch
            {
                3 => "TREINTA",
                4 => "CUARENTA",
                5 => "CINCUENTA",
                6 => "SESENTA",
                7 => "SETENTA",
                8 => "OCHENTA",
                9 => "NOVENTA",
                _ => ""
            };
            long resto = value % 10;
            return resto == 0 ? decena : $"{decena} Y {ConvertirEnteroALetras(resto)}";
        }

        if (value < 1000)
        {
            string centena = (value / 100) switch
            {
                1 => "CIENTO",
                2 => "DOSCIENTOS",
                3 => "TRESCIENTOS",
                4 => "CUATROCIENTOS",
                5 => "QUINIENTOS",
                6 => "SEISCIENTOS",
                7 => "SETECIENTOS",
                8 => "OCHOCIENTOS",
                9 => "NOVECIENTOS",
                _ => ""
            };
            long resto = value % 100;
            return resto == 0 ? centena : $"{centena} {ConvertirEnteroALetras(resto)}";
        }

        if (value < 1000000)
        {
            long miles = value / 1000;
            long resto = value % 1000;
            string prefix = miles == 1 ? "MIL" : $"{ConvertirEnteroALetras(miles)} MIL";
            return resto == 0 ? prefix : $"{prefix} {ConvertirEnteroALetras(resto)}";
        }

        if (value < 1000000000)
        {
            long millones = value / 1000000;
            long resto = value % 1000000;
            string prefix = millones == 1 ? "UN MILLÓN" : $"{ConvertirEnteroALetras(millones)} MILLONES";
            return resto == 0 ? prefix : $"{prefix} {ConvertirEnteroALetras(resto)}";
        }

        return value.ToString();
    }

    private async Task InicializarTarifasBaseAsync(LegalTechDbContext db)
    {
        var tarifasBase = new List<TarifaArancelaria>
        {
            new() { Concepto = "Búsqueda Preliminar de Antecedentes y Dictamen", EtapaProcesal = "Apertura", HonorariosHNL = 2500.00m, HonorariosUSD = 100.00m, GastosOficialesHNL = 0.00m, GastosOficialesUSD = 0.00m, AplicaISV = true, Descripcion = "Análisis fonético, visual y conceptual previo en base de datos DIGEPIH", Activo = true },
            new() { Concepto = "Solicitud y Trámite Completo de Registro (por Clase Niza)", EtapaProcesal = "Presentación", HonorariosHNL = 8500.00m, HonorariosUSD = 350.00m, GastosOficialesHNL = 1400.00m, GastosOficialesUSD = 55.00m, AplicaISV = true, Descripcion = "Incluye radicación, timbres CAH (L 50), tasa DIGEPIH (L 700) y La Gaceta (L 650)", Activo = true },
            new() { Concepto = "Contestación de Prevención de Forma (30 días)", EtapaProcesal = "Examen", HonorariosHNL = 3500.00m, HonorariosUSD = 140.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Subsanación de requisitos formales, clasificación de Niza o poderes", Activo = true },
            new() { Concepto = "Contestación de Objeción de Fondo (60 días)", EtapaProcesal = "Examen", HonorariosHNL = 7000.00m, HonorariosUSD = 280.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Defensa jurídica ante reparos de distintividad o semejanza (Arts. 83 y 84 LPI)", Activo = true },
            new() { Concepto = "Gestión de Publicaciones y Depósito de Diarios Físicos", EtapaProcesal = "Publicación", HonorariosHNL = 2000.00m, HonorariosUSD = 80.00m, GastosOficialesHNL = 650.00m, GastosOficialesUSD = 25.00m, AplicaISV = true, Descripcion = "Seguimiento a 3 avisos ENAG y entrega de ejemplares físicos para evitar abandono", Activo = true },
            new() { Concepto = "Defensa ante Oposición de Terceros (10 días)", EtapaProcesal = "Oposición", HonorariosHNL = 10000.00m, HonorariosUSD = 400.00m, GastosOficialesHNL = 50.00m, GastosOficialesUSD = 2.00m, AplicaISV = true, Descripcion = "Contestación, evacuación de medios probatorios y conclusiones de primera instancia", Activo = true },
            new() { Concepto = "Interposición de Oposición en Ataque (30 días)", EtapaProcesal = "Oposición", HonorariosHNL = 12000.00m, HonorariosUSD = 480.00m, GastosOficialesHNL = 500.00m, GastosOficialesUSD = 20.00m, AplicaISV = true, Descripcion = "Oposición contra solicitud lesiva de tercero publicada en La Gaceta", Activo = true },
            new() { Concepto = "Concesión, Certificado Oficial y Solvencia P360", EtapaProcesal = "Concesión", HonorariosHNL = 3000.00m, HonorariosUSD = 120.00m, GastosOficialesHNL = 500.00m, GastosOficialesUSD = 20.00m, AplicaISV = true, Descripcion = "Retiro de certificado original de DIGEPIH, auditoría P360 y custodia en bóveda", Activo = true },
            new() { Concepto = "Acción de Cancelación por No Uso (3 años)", EtapaProcesal = "Litigios", HonorariosHNL = 12500.00m, HonorariosUSD = 500.00m, GastosOficialesHNL = 750.00m, GastosOficialesUSD = 30.00m, AplicaISV = true, Descripcion = "Auditoría en redes sociales, constancia de no rehabilitación y demanda de cancelación", Activo = true },
            new() { Concepto = "Acción de Nulidad de Registro Marcario", EtapaProcesal = "Litigios", HonorariosHNL = 15000.00m, HonorariosUSD = 600.00m, GastosOficialesHNL = 1000.00m, GastosOficialesUSD = 40.00m, AplicaISV = true, Descripcion = "Demanda por registro otorgado en contravención a la Ley de Propiedad Industrial", Activo = true },
            new() { Concepto = "Recurso de Reposición (10 días) / Apelación (3 días)", EtapaProcesal = "Litigios", HonorariosHNL = 8000.00m, HonorariosUSD = 320.00m, GastosOficialesHNL = 100.00m, GastosOficialesUSD = 4.00m, AplicaISV = true, Descripcion = "Impugnación administrativa ante DIGEPIH y Superintendencia de Recursos", Activo = true }
        };

        db.TarifasArancelarias.AddRange(tarifasBase);
        await db.SaveChangesAsync();
    }
}
