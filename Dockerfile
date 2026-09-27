# Etapa 1: Compilación con el SDK oficial de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj y restaurar paquetes NuGet
COPY ["LegalTech.Web/LegalTech.Web/LegalTech.Web.csproj", "LegalTech.Web/LegalTech.Web/"]
RUN dotnet restore "LegalTech.Web/LegalTech.Web/LegalTech.Web.csproj"

# Copiar el resto del código fuente y publicar
COPY . .
WORKDIR "/src/LegalTech.Web/LegalTech.Web"
RUN dotnet publish "LegalTech.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen de ejecución (Runtime liviano de ASP.NET Core 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer puerto HTTP
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LegalTech.Web.dll"]
