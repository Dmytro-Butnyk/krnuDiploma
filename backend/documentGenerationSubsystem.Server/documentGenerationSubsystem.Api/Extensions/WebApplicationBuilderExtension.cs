using Microsoft.AspNetCore.ResponseCompression;

namespace documentGenerationSubsystem.Api.Extensions;

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

    public static void ValidateDIOnBuild(this WebApplicationBuilder builder)
    {
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
    }
}