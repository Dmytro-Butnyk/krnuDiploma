using Microsoft.AspNetCore.ResponseCompression;

namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationBuilderExtension
{
    private static WebApplicationBuilder AddIdentityModule(this WebApplicationBuilder builder, string connectionString)
    {
        // string tokenIssuer = builder.Configuration.GetOrThrow("TOKEN_ISSUER");
        // string tokenAudience = builder.Configuration.GetOrThrow("TOKEN_AUDIENCE");
        // string tokenKey = builder.Configuration.GetOrThrow("TOKEN_KEY");
        // string tokenLifetime = builder.Configuration.GetOrThrow("TOKEN_LIFETIME");
        //
        // builder.Services.AddIdentityModule(connectionString, tokenIssuer, tokenAudience, tokenKey, tokenLifetime);
        
        return builder;
    }

    public static WebApplicationBuilder AddAppCache(this WebApplicationBuilder builder)
    {
        
        return builder;
    }
    
    public static WebApplicationBuilder AddCors(this WebApplicationBuilder builder)
    {
        // builder.Services.AddCors(options =>
        // {
        //     options.AddPolicy("AllowNextClient", policy =>
        //     {
        //         policy.WithOrigins("http://localhost:3000")
        //             .AllowAnyHeader()
        //             .AllowAnyMethod()
        //             .AllowCredentials();
        //     });
        // });

        return builder;
    }
    
    public static WebApplicationBuilder AddSwaggerJwtBearer(this WebApplicationBuilder builder)
    {

        return builder;
    }
    
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

    public static WebApplicationBuilder AddLoggingToMongoDb(this WebApplicationBuilder builder)
    {
        
        return builder;
    }
    //
    // public static WebApplicationBuilder AddRateLimiter(this WebApplicationBuilder builder)
    // {
    //     
    // }
    //
    // private static void AddRateLimiterPolicy(RateLimiterOptions options, string policyName, int limit,
    //     TimeSpan expiration)
    // {
    //     
    // }
    //
    // public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    // {
    //     
    // }
    
    
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
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
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