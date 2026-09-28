using System.Collections.Generic;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Catálogo de roles estandarizados para el sistema LegalTech Honduras.
/// Implementa Role-Based Access Control (RBAC) estricto.
/// </summary>
public static class RolesSistema
{
    public const string SocioDirector = "SocioDirector";
    public const string AbogadoSenior = "AbogadoSenior";
    public const string Paralegal = "Paralegal";
    public const string Finanzas = "Finanzas";

    public static readonly List<string> TodosLosRoles = new()
    {
        SocioDirector,
        AbogadoSenior,
        Paralegal,
        Finanzas
    };

    public static string ObtenerNombreLegible(string rol) => rol switch
    {
        SocioDirector => "Socio Director (Administrador)",
        AbogadoSenior => "Abogado Senior de Marcas",
        Paralegal => "Paralegal / Procurador Judicial",
        Finanzas => "Finanzas y Facturación SAR",
        _ => rol
    };

    public static string ObtenerBadgeColor(string rol) => rol switch
    {
        SocioDirector => "badge bg-warning text-dark border border-warning",
        AbogadoSenior => "badge bg-primary text-white",
        Paralegal => "badge bg-info text-dark",
        Finanzas => "badge bg-success text-white",
        _ => "badge bg-secondary"
    };

    public static string ObtenerIcono(string rol) => rol switch
    {
        SocioDirector => "shield",
        AbogadoSenior => "gavel",
        Paralegal => "assignment_ind",
        Finanzas => "account_balance_wallet",
        _ => "person"
    };
}
