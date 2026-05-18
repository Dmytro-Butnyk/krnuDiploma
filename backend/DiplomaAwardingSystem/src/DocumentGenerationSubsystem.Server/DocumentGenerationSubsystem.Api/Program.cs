using System.Globalization;
using Core.Api.ExceptionHandlers;
using Core.Api.Extensions;
using Core.Api.Middleware;
using DocumentGenerationSubsystem.Api;
using DocumentGenerationSubsystem.Api.Extensions;
using DotNetEnv;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// ==============================================================================
// 1. BOOTSTRAP & CONFIGURATION
// Initialize early logging to catch setup errors before the DI container is built.
// ==============================================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Env.Load("../../../.env", new LoadOptions(onlyExactPath: true));

    var builder = WebApplication.CreateBuilder(args);

    builder.ValidateDIOnBuild();
    builder.AddResponseCompression();

    // Replace the default Microsoft logger with full Serilog capabilities
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ==============================================================================
    // 2. DEPENDENCY INJECTION (SERVICES PORTFOLIO)
    // Registering abstract dependencies. Order mostly DOES NOT matter here.
    // ==============================================================================
    
    // 2.1. Security & Validation
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
    builder.Services.AddFluentValidationFromAssemblyMarker<AssemblyMarker>();
    builder.Services.AddCustomCors(builder.Configuration);

    // 2.2. Error Handling & Observability
    builder.Services.AddExceptionHandler<ExceptionHandler>();
    builder.Services.AddProblemDetails();

    // 2.3. Infrastructure & Database
    var connectionString = builder.Configuration["DataBase"];
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is missing in configuration.");
    }
    
    builder.Services.AddPostgresql<AssemblyMarker>(connectionString);

    // 2.4. Application Logic (Auto-discovery via Scrutor)
    builder.Services.AddScrutorFromAssemblyMarker<AssemblyMarker>();

    // 2.5. API Documentation
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "DocumentGeneration API",
                Version = "v1",
                Description = "API for dynamic document generation (Clean Architecture)",
                Contact = new OpenApiContact { Name = "Backend Team" }
            };
            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    // ==============================================================================
    // 3. HTTP REQUEST PIPELINE (MIDDLEWARE)
    // Order MATTERS CRITICALLY! This is the "Matryoshka" (Russian Doll) model.
    // ==============================================================================

    // LAYER 1: Global Error Catching (Outermost shell)
    app.UseExceptionHandler();
    app.UseResponseCompression();
    app.UseRouting();
    app.UseCors(BuilderExtensions.CorsPolicyName);

    // LAYER 2: Request Logging (Needs to know if the ExceptionHandler changed status to 500)
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, ex) =>
            httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError || ex != null
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    // LAYER 3: Network & Security Headers
    app.UseHttpsRedirection();

    // LAYER 4: Identity & Permissions (Who are you? What can you do?)
    app.UseAuthentication();
    app.UseAuthorization();

    // LAYER 5: Context Enrichment (Requires Identity from Layer 4 to log UserId)
    app.UseMiddleware<LogEnrichmentMiddleware>();

    // ==============================================================================
    // 4. ENDPOINTS EXECUTION (INNERMOST CORE)
    // ==============================================================================
    app.MapAllEndpoints();

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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
