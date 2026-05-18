# Core Project Guidelines

## Scope

`src/Core` contains shared domain, infrastructure, and API primitives used by server applications.

- `Core.Domain` owns entities, enums, result pattern, and DI marker interfaces.
- `Core.Infrastructure` owns `DbDocGenContext`, EF Core configurations, migrations, and infrastructure persistence.
- `Core.Api` owns reusable ASP.NET Core API helpers, exception handling, result-to-HTTP mapping, and shared middleware.

## Entity Rules

- Entities inherit from `BaseEntity` when they need an integer primary key.
- Mutable scalar properties use `set` because the application supports user-driven edits.
- Collection navigation properties stay initialized with `HashSet<T>`.
- Keep relationship comments short and factual, for example `// 1-to-1 with Student`.
- Do not return domain entities directly from endpoints. Project to DTOs in server projects.

## EF Core Rules

- Configure entity scalar fields in that entity's own `IEntityTypeConfiguration<T>`.
- Configure relationships from the parent/principal configuration when that matches the existing model.
- Use explicit `DeleteBehavior` for every relationship.
- Keep migrations in `Core.Infrastructure`.
- `DbDocGenContext` is the shared context for the current backend.
- Server projects register their own `IEntityConfigurationMarker` so `DbDocGenContext` can apply project-local configurations.

## Shared API Rules

- Put reusable ASP.NET Core extensions in `Core.Api`.
- Keep project-specific endpoint mapping in each server project.
- Reuse `Core.Api.ExceptionHandlers.ExceptionHandler`.
- Reuse `Core.Api.Middleware.LogEnrichmentMiddleware` for correlation and user log context.
- Reuse `Core.Api.Extensions.BuilderExtensions` for CORS, PostgreSQL, Scrutor, FluentValidation, response compression, and DI validation.

## Dependency Injection

- Use marker interfaces from `Core.Domain.DependencyInjectionInterfaces`:
  - `ITransientService`
  - `IScopedService`
  - `ISingletonService`
- Do not introduce repository abstractions over EF Core unless explicitly requested.
- Do not introduce MediatR, AutoMapper, or Mapster by default.

## Verification

Prefer narrow builds when possible:

```powershell
dotnet build src\Core\Core.Api\Core.Api.csproj
dotnet build src\Core\Core.Infrastructure\Core.Infrastructure.csproj
```
