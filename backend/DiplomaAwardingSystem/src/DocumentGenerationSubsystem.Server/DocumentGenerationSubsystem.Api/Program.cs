using DocumentGenerationSubsystem.Api.Extensions;
using DocumentGenerationSubsystem.Infrastructure;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

LoadOptions loadOptions = new(onlyExactPath: true);
Env.Load("../../../.env", loadOptions);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication();

string? connectionString = builder.Configuration["DataBase"];

builder.Services.AddDbContext<DbDocGenContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddProblemDetails();
builder.Services.AddScrutor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
