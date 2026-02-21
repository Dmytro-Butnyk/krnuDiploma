using Microsoft.AspNetCore.ResponseCompression;

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

            // Register data providers
            // .AddClasses(classes => classes.AssignableTo<IDataProvider>())
            // .AsImplementedInterfaces() // IDataProvider
            // .WithScopedLifetime()

            // Register services with and without interfaces
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)))
            .AsImplementedInterfaces() 
            .AsSelf()
            .WithScopedLifetime()
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
