# Diploma Control System Agent Guide

## Scope

`src/DiplomaControlSystem.Server` is the API for diploma process control and accounting. It is intended for secretary/operator workflows over students, groups, qualification works, defences, commissions, checklists, and archive-related data.

## Project Shape

- Use ASP.NET Core Minimal APIs.
- Use Vertical Slice Architecture.
- API project: `DiplomaControlSystem.Api`.
- Shared API startup helpers come from `Core.Api`.
- EF Core DbContext is `DbDocGenContext` from `Core.Infrastructure`.
- Route mapping is centralized in `DiplomaControlSystem.Api/Extensions/WebApplicationExtension.cs`.

## Folder Boundaries

- `Features/<Area>/<Action>.cs` contains endpoint slices only.
- A feature file should contain the endpoint mapping, request DTOs used only by that endpoint, endpoint-specific validator, and handler.
- Do not place shared helpers, reusable validators, shared interfaces, mappers, or cross-feature DTOs in `Features`.
- Put public API contracts and DTOs reused by more than one feature under `Contracts/<Area>`.
- Put reusable feature-area support under `Infrastructure/<Area>`, for example shared validators, request interfaces, normalization helpers, mappers, and business-rule helpers.
- Endpoint-specific request DTOs can stay nested in their feature file. Move them to `Contracts` only when another feature needs the same request/response shape.
- Prefer dependencies flowing from features to `Contracts` and `Infrastructure`, not from one feature area into another feature area's contracts by accident.

## Commission Heads

- Commission head specialty is a free-text string entered by the secretary.
- Do not require commission head specialty to match the secretary specialty.
- Do not require commission head specialty to exist in the `Specialties` table.
- Shared commission head DTOs live in `Contracts/CommissionHeads`.
- Shared commission head request validation/support lives in `Infrastructure/CommissionHeads`.

## Endpoint Rules

- Routes start under `/api`.
- Use kebab-case routes, for example `/api/students/{id:int}`.
- Use `TypedResults`, not untyped `Results`.
- Return DTOs, not EF/domain entities.
- Use `.WithTags(...)`, `.WithSummary(...)`, and `.Produces...()` metadata.
- For user input, use FluentValidation validators registered by assembly scanning.
- Use explicit typed result unions such as `Results<Ok<T>, ProblemHttpResult, ValidationProblem>`.
- Keep handlers private nested classes when they are only used by one feature.

## Data Access

- Use `DbDocGenContext` directly in handlers.
- Use `.AsNoTracking()` for read-only queries.
- Project with `.Select(...)` instead of loading entire entities for list/detail responses.
- Pass `CancellationToken` to async EF Core operations.
- For updates, load the entity, mutate fields, and call `SaveChangesAsync`.
- Use `EF.Functions.ILike` for case-insensitive PostgreSQL string comparisons when matching persisted text.

## Dependency Injection

- Services and handlers that need DI can implement:
  - `IScopedService`
  - `ITransientService`
  - `ISingletonService`
- Scrutor registration is configured through `AssemblyMarker`.
- Validators do not implement service marker interfaces.

## Result Pattern

- Use `Result`, `Result<T>`, and `ErrorDetails` from `Core.Domain.ResultPattern`.
- Return existing nested failures as-is with `return result.ErrorDetails;`.
- Convert failures to HTTP responses in endpoints with `result.ToProblemDetails()`.
- Use stable error codes in the form `Domain.ErrorName`, for example `CommissionHead.NotFound`.

## Application Startup

- Use shared startup helpers from `Core.Api.Extensions`.
- Use Scalar at `/docs`.
- Use Serilog for logging.
- Do not call `DatabaseSeeder.SeedAsync(context)` from this project.
- Database connection string is expected from `.env` key `DataBase`.

## Verification

```powershell
dotnet build src\DiplomaControlSystem.Server\DiplomaControlSystem.Api\DiplomaControlSystem.Api.csproj /clp:ErrorsOnly -v q
```
