# SkyCrop Platform

SkyCrop Platform provides the REST API for managing agricultural plots, crops, drones, flights, diagnoses and reports within the SkyCrop application.

Developed by GreenTech using ASP.NET Core, Entity Framework Core and MySQL.

## Technology stack

- C# 14 and .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- Cortex.Mediator
- OpenAPI and Scalar
- GitHub and GitFlow

## Bounded contexts

| Context | Responsibility |
| --- | --- |
| Plots | Agricultural plot registration and queries. |
| Crops | Crop registration and queries. |
| Drones | Drone registration, flight routes, flights and aerial image queries. |
| Diagnoses | Diagnosis generation, history, results and map queries. |
| Reports | Report creation and queries. |
| Shared | Persistence, error handling, localization and shared infrastructure. |

Diagnosis image analysis currently uses a simulated implementation.

## Architecture

Each bounded context organizes its responsibilities into the following layers:

- **Domain:** aggregates, entities, value objects, commands, queries, events and repository contracts.
- **Application:** command services, query services and integration contracts.
- **Infrastructure:** persistence, repository implementations, external adapters and dependency injection.
- **Interfaces:** REST controllers, resources and resource assemblers.

Entity Framework Core manages persistence and database migrations. Cortex.Mediator supports command processing and event handling.

## Repository structure

```text
skycrop-platform/
├── GreenTech.SkyCrop.Platform/
│   ├── Crops/
│   ├── Diagnoses/
│   ├── Drones/
│   ├── Plots/
│   ├── Reports/
│   ├── Shared/
│   ├── Resources/
│   ├── Migrations/
│   └── Program.cs
├── docs/
├── tests/
└── skycrop-platform.sln
```

## Prerequisites

- .NET 10 SDK
- A running MySQL server
- Git

The application requires a valid database connection before startup.

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/upc-pre-202620-1asi0730-8093-greentech/skycrop-platform.git
cd skycrop-platform
git switch develop
```

### 2. Configure the database connection

Set `ConnectionStrings:DefaultConnection` using local configuration or an environment variable.

Example connection string:

```text
server=localhost;port=3306;user=<database-user>;password=<database-password>;database=greentech-db
```

**PowerShell:**

```powershell
$env:ConnectionStrings__DefaultConnection = "server=localhost;port=3306;user=<database-user>;password=<database-password>;database=greentech-db"
```

**Bash or Zsh:**

```bash
export ConnectionStrings__DefaultConnection='server=localhost;port=3306;user=<database-user>;password=<database-password>;database=greentech-db'
```

Replace the placeholders with the database credentials for the environment.  
Keep deployment credentials outside version control.

### 3. Restore dependencies and build

```bash
dotnet restore skycrop-platform.sln
dotnet build skycrop-platform.sln
```

### 4. Start the application

```bash
dotnet run --project GreenTech.SkyCrop.Platform --launch-profile https
```

The HTTPS development profile uses:

- HTTPS: `https://localhost:7172`
- HTTP: `http://localhost:5231`

The application applies pending Entity Framework Core migrations during startup.  
The configured database account must have the permissions required to apply them.

## API documentation

When running in the Development environment:

- Interactive documentation: `https://localhost:7172/scalar`
- OpenAPI document: `https://localhost:7172/openapi/v1.json`

The root URL redirects to Scalar in Development.

API documentation routes are currently enabled only in Development.

## API overview

All business endpoints use the `/api/v1` prefix.

| Resource | Base route | Available operations |
| --- | --- | --- |
| Plots | `/api/v1/plots` | List, retrieve and create. |
| Crops | `/api/v1/crops` | List, retrieve and create. |
| Drones | `/api/v1/drones` | List, retrieve and create. |
| Flight routes | `/api/v1/flight-routes` | List, retrieve and create. |
| Flights | `/api/v1/flights` | List, retrieve and create. |
| Aerial images | `/api/v1/aerial-images` | List and retrieve. |
| Diagnoses | `/api/v1/diagnoses` | List, retrieve, generate and retrieve map information. |
| Reports | `/api/v1/reports` | List, retrieve and create. |

Flight routes and aerial images also support the compatibility paths `/api/v1/flightRoutes` and `/api/v1/aerialImages`.

Refer to the generated OpenAPI document for request bodies, query parameters, response schemas and endpoint requirements.

## Localization and error handling

The application supports English and Spanish localization.

Supported cultures:

- `en-US`
- `en`
- `es-419`
- `es`

Clients may request a supported language through the `Accept-Language` header.

The shared infrastructure provides centralized exception handling and Problem Details responses.

## Frontend integration

The SkyCrop frontend uses `VITE_SKYCROP_API_URL` as its API base URL.

For local integration:

```env
VITE_SKYCROP_API_URL=https://localhost:7172/api/v1
```

Restart the frontend development server after changing its environment variables.

## Verification

Run the available domain checks from the repository root:

```bash
dotnet run --project tests/SkyCrop.Drones.Checks
dotnet run --project tests/SkyCrop.Diagnoses.Checks
```

Additional drone verification instructions are available in [Drone management](docs/drones.md).

## Deployment configuration

The hosting environment must support .NET 10 and provide access to MySQL.

Set the runtime environment to `Production` and configure the database connection through `ConnectionStrings__DefaultConnection`.

Alternatively, the existing production configuration accepts:

- `DATABASE_URL`
- `DATABASE_PORT`
- `DATABASE_USER`
- `DATABASE_PASSWORD`
- `DATABASE_SCHEMA`

`JWT_SECRET` is referenced by the production token configuration.

Pending database migrations run when the application starts. Confirm database connectivity and migration permissions before starting a new deployment.

## Development workflow

The repository follows GitFlow:

- `main`: released versions.
- `develop`: integration of ongoing development.
- `feature/*`: individual features.
- `release/*`: release preparation.
- `hotfix/*`: corrections to released versions.

Use Conventional Commits and keep each commit focused on a specific change. Code, identifiers and technical documentation use English.

## Related repositories

- [Frontend application](https://github.com/upc-pre-202620-1asi0730-8093-greentech/skycrop-webapp)
- [Landing page](https://github.com/upc-pre-202620-1asi0730-8093-greentech/skycrop-website)
- [Project report](https://github.com/upc-pre-202620-1asi0730-8093-greentech/skycrop-report)
- [Mock API](https://github.com/upc-pre-202620-1asi0730-8093-greentech/skycrop-mock-api)
