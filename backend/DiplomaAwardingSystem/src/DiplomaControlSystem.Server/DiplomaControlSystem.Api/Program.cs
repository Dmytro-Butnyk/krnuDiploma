using System.Globalization;
using System.Text;
using Core.Api.ExceptionHandlers;
using Core.Api.Extensions;
using Core.Api.Middleware;
using DiplomaControlSystem.Api;
using DiplomaControlSystem.Api.Extensions;
using DiplomaControlSystem.Api.Infrastructure.Auth;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Env.Load("../../../.env", new LoadOptions(onlyExactPath: true));

    var builder = WebApplication.CreateBuilder(args);

    builder.ValidateDIOnBuild();
    builder.AddResponseCompression();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.Configure<GoogleAuthOptions>(
        builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection(JwtOptions.SectionName));

    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>() ?? new JwtOptions();

    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    {
        throw new InvalidOperationException("JWT signing key is missing or shorter than 32 bytes.");
    }

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.Secretary, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(AuthRoles.Secretary));

        options.AddPolicy(AuthPolicies.SuperSecretary, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(AuthRoles.SuperSecretary));
    });
    builder.Services.AddHttpClient();
    builder.Services.AddFluentValidationFromAssemblyMarker<AssemblyMarker>();
    builder.Services.AddCustomCors(builder.Configuration);

    builder.Services.AddExceptionHandler<ExceptionHandler>();
    builder.Services.AddProblemDetails();

    var connectionString = builder.Configuration["DataBase"];
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is missing in configuration.");
    }

    builder.Services.AddPostgresql<AssemblyMarker>(connectionString);
    builder.Services.AddScrutorFromAssemblyMarker<AssemblyMarker>();

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "DiplomaControlSystem API",
                Version = "v1",
                Description = "API for diploma process control and accounting",
                Contact = new OpenApiContact { Name = "Backend Team" }
            };

            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseResponseCompression();
    app.UseRouting();
    app.UseCors(BuilderExtensions.CorsPolicyName);

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, ex) =>
            httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError || ex != null
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<LogEnrichmentMiddleware>();

    app.MapAllEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/docs", options =>
        {
            options.WithTitle("DiplomaControlSystem API")
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
