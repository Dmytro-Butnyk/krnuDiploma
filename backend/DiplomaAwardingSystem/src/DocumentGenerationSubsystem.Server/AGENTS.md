# Document Generation Subsystem Guidelines

## Scope

`src/DocumentGenerationSubsystem.Server` is the API for document template management and dynamic document generation. Treat it as stable unless a task explicitly targets document generation.

## Project Boundaries

- Do not move document-specific logic into `DiplomaControlSystem.Server`.
- Keep document generation concerns here:
  - template upload/download/update/delete
  - template scanning
  - document generation engine
  - allowed entity registry for document generation
  - document template entity/configuration
- Packages such as `MiniWord`, `FastMember`, and `Microsoft.IO.RecyclableMemoryStream` are specific to this subsystem.

## Architecture

- Use ASP.NET Core Minimal APIs.
- Use Vertical Slice Architecture under `DocumentGenerationSubsystem.Api/Features`.
- Keep endpoint mapping in `Extensions/WebApplicationExtension.cs`.
- Use `DbDocGenContext` directly for data access.
- Use Scrutor marker interfaces for injected handlers/services.

## Endpoint Rules

- Return DTOs or file results, not domain entities.
- Use `TypedResults`.
- Use FluentValidation for request validation.
- Multipart upload endpoints should use `[FromForm]` and `.DisableAntiforgery()`.
- Pass `CancellationToken` to async work.

## Stability Rule

Avoid broad refactors in this project while working on `DiplomaControlSystem.Server`. If shared infrastructure is improved in `Core.Api`, migrate this subsystem separately and deliberately.

## Verification

```powershell
dotnet build src\DocumentGenerationSubsystem.Server\DocumentGenerationSubsystem.Api\DocumentGenerationSubsystem.Api.csproj
```
