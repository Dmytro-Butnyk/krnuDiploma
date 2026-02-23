using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.DependencyInjectionInterfaces;
using DocumentGenerationSubsystem.Infrastructure;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationBuilderExtension
{
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

    public static IServiceCollection AddPostgresql(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DbDocGenContext>(options =>
            options.UseNpgsql(connectionString));
        
        services.AddScoped<IDbDocGenContext>(provider => 
            provider.GetRequiredService<DbDocGenContext>());
        
        return services;
    }

    /// <summary>
    /// Configures Scrutor to automatically register services from the application's assemblies based on naming conventions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScrutor(this IServiceCollection services)
    {
        services.Scan(scan => scan
                
            // Follow to assemblies with marker classes
            .FromAssemblies(
                typeof(AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(Infrastructure.AssemblyMarker).Assembly
            )
            
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .AsSelf()
            .WithTransientLifetime()
            
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .AsImplementedInterfaces()
            .AsSelf()
            .WithScopedLifetime()
            
            .AddClasses(classes => classes.AssignableTo<ISingletonService>())
            .AsImplementedInterfaces()
            .AsSelf()
            .WithSingletonLifetime()
        );

        return services;
    }

    public static void ValidateDIOnBuild(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
    }
}
