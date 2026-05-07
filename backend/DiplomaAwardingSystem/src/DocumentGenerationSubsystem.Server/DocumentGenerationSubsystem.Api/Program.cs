using Core.Infrastructure;
using Core.Infrastructure.Seeding;
using DocumentGenerationSubsystem.Api.Extensions;
using DotNetEnv;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

Env.Load("../../../.env", new LoadOptions(onlyExactPath: true));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFluentValidation();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration["DataBase"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is missing in configuration.");
}

builder.Services.AddPostgresql(connectionString);

builder.Services.AddScrutor();

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

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DbDocGenContext>();
    await DatabaseSeeder.SeedAsync(context);
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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
