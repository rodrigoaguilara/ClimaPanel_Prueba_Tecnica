# ClimaPanel - prueba técnica .NET

Aplicación ASP.NET Core MVC con interfaz gráfica, Entity Framework Core, SQLite
y consumo de la API pública Open-Meteo.

## Documento oficial

Lea primero `GUIA_POSTULANTE.pdf`. Ese documento contiene el alcance, las
reglas, los niveles evaluados, la funcionalidad nueva, la pauta y el proceso de
entrega.

## Requisitos

- SDK de .NET 8.
- Git.
- Navegador y conexión a Internet.

No necesita instalar SQLite, Docker, SQL Server ni una VPN.

## Validación inicial

En Windows:

```text
VALIDAR.cmd
```

En cualquier sistema:

```bash
dotnet restore
dotnet build
dotnet test
```

## Ejecución

```bash
dotnet run --project src/ClimaPanel.Web
```

Abra `http://localhost:5085`.

La base local se crea en `src/ClimaPanel.Web/data/climapanel.db`.

## Reinicio de la base

Windows PowerShell:

```powershell
./scripts/reset-db.ps1
```

Linux/macOS:

```bash
./scripts/reset-db.sh
```

## Funcionalidades implementadas

- Integración asincrónica con Open-Meteo mediante HttpClient e inyección de dependencias.
- Control de timeout, cancelación y errores del proveedor externo.
- Separación de favoritos por usuario.
- Prevención de ciudades duplicadas por usuario mediante restricción en SQLite.
- Caché independiente por ciudad con estados LIVE, CACHE y STALE.
- Actualización manual del clima.
- Paginación realizada desde la consulta a SQLite.
- Alertas meteorológicas persistentes por temperatura, humedad, precipitación y viento.
- Creación, evaluación, activación, desactivación y eliminación de alertas.
- Máximo de cinco alertas activas por ciudad.
