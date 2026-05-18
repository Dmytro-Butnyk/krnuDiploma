using Core.Domain;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Api.Extensions;

public static class BuilderExtensions
{
    public const string CorsPolicyName = "DefaultCorsPolicy";

    public static WebApplicationBuilder AddResponseCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        return builder;
    }

    public static void ValidateDIOnBuild(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
    }

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:5173"];

        return services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });
    }

    public static IServiceCollection AddPostgresql<TAssemblyMarker>(
        this IServiceCollection services,
        string connectionString)
        where TAssemblyMarker : class, IEntityConfigurationMarker
    {
        services.AddSingleton<IEntityConfigurationMarker, TAssemblyMarker>();

        services.AddDbContext<DbDocGenContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddScrutorFromAssemblyMarker<TAssemblyMarker>(
        this IServiceCollection services)
    {
        return services.Scan(scan => scan
            .FromAssemblies(typeof(TAssemblyMarker).Assembly)
            .AddClasses(classes => classes.AssignableTo<ITransientService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .AsSelf()
            .WithTransientLifetime()
            .AddClasses(classes => classes.AssignableTo<IScopedService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .AsSelf()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo<ISingletonService>(), publicOnly: false)
            .AsImplementedInterfaces()
            .AsSelf()
            .WithSingletonLifetime());
    }

    public static IServiceCollection AddFluentValidationFromAssemblyMarker<TAssemblyMarker>(
        this IServiceCollection services)
    {
        return services.AddValidatorsFromAssembly(
            assembly: typeof(TAssemblyMarker).Assembly,
            includeInternalTypes: true,
            lifetime: ServiceLifetime.Scoped);
    }
}
