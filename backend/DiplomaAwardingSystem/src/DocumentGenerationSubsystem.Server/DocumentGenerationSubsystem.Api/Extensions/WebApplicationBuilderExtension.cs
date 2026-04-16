using Core.Domain;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Infrastructure;
using FluentValidation;
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

    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPostgresql(string connectionString)
        {
            services.AddSingleton<IEntityConfigurationMarker, AssemblyMarker>();

            services.AddDbContext<DbDocGenContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }

        /// <summary>
        /// Configures Scrutor to automatically register services from the application's assemblies based on naming conventions.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddScrutor() =>
            services.Scan(scan => scan

                // Follow to assemblies with marker classes
                .FromAssemblies(
                    typeof(AssemblyMarker).Assembly
                )
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
                .WithSingletonLifetime()
            );

        public IServiceCollection AddFluentValidation() =>
            services.AddValidatorsFromAssembly(
                assembly: typeof(AssemblyMarker).Assembly, 
                includeInternalTypes: true, 
                lifetime: ServiceLifetime.Scoped);
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
