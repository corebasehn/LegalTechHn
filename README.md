# LegalTech Honduras - Sistema de Gestión de Marcas y Litigios

Sistema integral para la gestión de expedientes marcarios, seguimiento de plazos procesales perentorios ante la DIGEPIH / IP (conforme a la Ley de Propiedad Industrial y Ley de Procedimiento Administrativo de Honduras), y control de controversias y litigios.

## Tecnologías
- **Framework:** .NET 10 (ASP.NET Core Blazor Web App)
- **Modo de renderizado:** Blazor Interactive Server
- **Componentes UI:** Radzen Blazor Components
- **ORM & Base de Datos:** Entity Framework Core con SQLite
- **Contenedores:** Docker (Multi-stage build)

## Ejecución Local
```bash
cd LegalTech.Web/LegalTech.Web
dotnet run
```
La aplicación creará automáticamente la base de datos SQLite y sembrará los feriados oficiales de Honduras y clientes de prueba.
