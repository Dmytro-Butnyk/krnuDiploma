namespace documentGenerationSubsystem.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers application-specific services into the dependency injection container.</summary>
    public static IServiceCollection AddScrutorDiScanning(this IServiceCollection services)
    {
        services.Scan(scan => scan
            // Follow to assemblies with marker classes
            .FromAssemblies(
                typeof(AssemblyMarker).Assembly,
                typeof(documentGenerationSubsystem.Application.AssemblyMarker).Assembly,
                typeof(documentGenerationSubsystem.Domain.AssemblyMarker).Assembly,
                typeof(documentGenerationSubsystem.Infrastructure.AssemblyMarker).Assembly
            )

            // Register data providers
            // .AddClasses(classes => classes.AssignableTo<IDataProvider>())
            // .AsImplementedInterfaces() // IDataProvider
            // .WithScopedLifetime()

            // Register services with and without interfaces
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces() 
            .AsSelf()
            .WithScopedLifetime()
        );

        return services;
    }
}