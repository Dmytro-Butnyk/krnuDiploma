using System.Reflection;
using DocumentGenerationSubsystem.Api.Endpoints;
using DocumentGenerationSubsystem.Api.Extensions;
using DocumentGenerationSubsystem.Infrastructure;
using DocumentGenerationSubsystem.Infrastructure.Seeding;
using DotNetEnv;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

// Load environment variables early
Env.Load("../../../.env", new LoadOptions(onlyExactPath: true));

var builder = WebApplication.CreateBuilder(args);

// --- Services Configuration ---

// Basic auth setup. Ready for JWT later, but does nothing strict right now.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Postgres configuration with null-check fail-fast
var connectionString = builder.Configuration["DataBase"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing in configuration.");
}

builder.Services.AddPostgresql(connectionString);

builder.Services.AddProblemDetails();
builder.Services.AddScrutor();

// 2. Native OpenAPI setup (Requires: Microsoft.AspNetCore.OpenApi)
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "DocumentGeneration API",
            Version = "v1",
            Description = "API for dynamic document generation (Clean Architecture)",
            Contact = new OpenApiContact { Name = "Backend Team" }
        };

        // Note: JWT Security definitions removed here since auth is not implemented yet.
        // Add them back when you introduce tokens.

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---

// 1. Global Exception Handler
app.UseExceptionHandler();

// --- Infrastructure / Seeding ---
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DbDocGenContext>();
    await DatabaseSeeder.SeedAsync(context);
}

// 2. Security / Routing
app.UseHttpsRedirection();

// 3. Identity Verification
app.UseAuthentication();
app.UseAuthorization();

// 4. Endpoints execution
app.MapDocumentGenerationEndpoints();

// 5. Documentation UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.MapScalarApiReference("/docs", options => 
    {
        options.WithTitle("DocumentGeneration API")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

await app.RunAsync();
