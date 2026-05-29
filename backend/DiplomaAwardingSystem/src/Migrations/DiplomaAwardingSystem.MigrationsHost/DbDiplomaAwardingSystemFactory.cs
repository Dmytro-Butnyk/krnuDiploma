using Core.Domain;
using Core.Infrastructure;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DiplomaControlAssemblyMarker = DiplomaControlSystem.Api.AssemblyMarker;
using DocumentGenerationAssemblyMarker = DocumentGenerationSubsystem.Api.AssemblyMarker;

namespace DiplomaAwardingSystem.MigrationsHost;

public sealed class DbDiplomaAwardingSystemFactory : IDesignTimeDbContextFactory<DbDocGenContext>
{
    private const string ConnectionArgumentPrefix = "--connection=";
    private const string CloudConnectionStringEnvironmentVariable = "DataBase";
    private const string LocalConnectionStringEnvironmentVariable = "LocalDataBase";

    public DbDocGenContext CreateDbContext(string[] args)
    {
        LoadEnvironmentFile();

        var connectionString = ResolveConnectionString(args);
        var options = new DbContextOptionsBuilder<DbDocGenContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(DbDiplomaAwardingSystemFactory).Assembly.GetName().Name))
            .Options;

        IEntityConfigurationMarker[] markers =
        [
            new DiplomaControlAssemblyMarker(),
            new DocumentGenerationAssemblyMarker()
        ];

        return new DbDocGenContext(options, markers);
    }

    private static string ResolveConnectionString(string[] args)
    {
        var connectionArgument = args.FirstOrDefault(argument =>
            argument.StartsWith(ConnectionArgumentPrefix, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(connectionArgument))
        {
            return connectionArgument[ConnectionArgumentPrefix.Length..];
        }

        return Environment.GetEnvironmentVariable(CloudConnectionStringEnvironmentVariable)
               ?? Environment.GetEnvironmentVariable(LocalConnectionStringEnvironmentVariable)
               ?? throw new InvalidOperationException(
                   $"Database connection string is missing. Set {CloudConnectionStringEnvironmentVariable} or " +
                   $"{LocalConnectionStringEnvironmentVariable}, or pass -- {ConnectionArgumentPrefix}<connection-string>.");
    }

    private static void LoadEnvironmentFile()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null)
        {
            var environmentFilePath = Path.Combine(currentDirectory.FullName, ".env");
            if (File.Exists(environmentFilePath))
            {
                Env.Load(environmentFilePath, new LoadOptions(onlyExactPath: true));
                return;
            }

            currentDirectory = currentDirectory.Parent;
        }
    }
}
