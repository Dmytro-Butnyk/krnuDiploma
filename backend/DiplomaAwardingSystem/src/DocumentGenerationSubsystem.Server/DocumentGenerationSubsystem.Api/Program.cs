using System.Reflection;
using DocumentGenerationSubsystem.Api.Endpoints;
using DocumentGenerationSubsystem.Api.Extensions;
using DotNetEnv;
using Microsoft.OpenApi.Models;

LoadOptions loadOptions = new(onlyExactPath: true);
Env.Load("../../../.env", loadOptions);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication();

string? connectionString = builder.Configuration["DataBase"];
if (connectionString is not null)
    builder.Services.AddPostgresql(connectionString);

builder.Services.AddProblemDetails();
builder.Services.AddScrutor();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DocumentGeneration API",
        Version = "v1",
        Description = "API для динамической генерации документов (Clean Architecture)",
        Contact = new OpenApiContact { Name = "Backend Team" }
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Введите JWT токен. Формат: Bearer {токен}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".", StringComparison.Ordinal));

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.MapDocumentGenerationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocumentGeneration API v1");
        
        options.DisplayRequestDuration(); 
        
        options.EnableTryItOutByDefault(); 
    });
}

app.UseHttpsRedirection();

app.Run();
