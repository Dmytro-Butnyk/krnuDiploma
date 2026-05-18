# Diploma Control System Guidelines

## Scope

`src/DiplomaControlSystem.Server` is the API for diploma process control and accounting. It is intended for secretary/operator workflows over students, groups, qualification works, defences, commissions, checklists, and archive-related data.

## Architecture

- Use ASP.NET Core Minimal APIs.
- Use Vertical Slice Architecture.
- Put feature code under `DiplomaControlSystem.Api/Features/<Area>/<Action>.cs`.
- A feature file should usually contain request/response DTOs, validator, endpoint, and handler.
- Keep route mapping centralized through `Extensions/WebApplicationExtension.cs`.

## Endpoint Rules

- Routes start under `/api`.
- Use kebab-case routes, for example `/api/students/{id:int}`.
- Use `TypedResults`, not untyped `Results`.
- Return DTOs, not EF/domain entities.
- Use `.WithTags(...)`, `.WithSummary(...)`, and `.Produces...()` metadata.
- For user input, use FluentValidation validators registered by assembly scanning.

## Data Access

- Use `DbDocGenContext` directly in handlers.
- Use `.AsNoTracking()` for read-only queries.
- Project with `.Select(...)` instead of loading entire entities for list/detail responses.
- Pass `CancellationToken` to async EF Core operations.
- For updates, load the entity, mutate fields, and call `SaveChangesAsync`.

## Dependency Injection

- Services and handlers that need DI can implement:
  - `IScopedService`
  - `ITransientService`
  - `ISingletonService`
- Scrutor registration is configured through `AssemblyMarker`.
- Validators do not implement service marker interfaces.

## Application Startup

- Use shared startup helpers from `Core.Api.Extensions`.
- Use Scalar at `/docs`.
- Use Serilog for logging.
- Do not call `DatabaseSeeder.SeedAsync(context)` from this project.
- Database connection string is expected from `.env` key `DataBase`.

## Verification

```powershell
dotnet build src\DiplomaControlSystem.Server\DiplomaControlSystem.Api\DiplomaControlSystem.Api.csproj
```
