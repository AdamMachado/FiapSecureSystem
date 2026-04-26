using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using ProcessingService.Infrastructure.Configuration.Options;

namespace ProcessingService.Infrastructure.Persistence.Context;

public sealed class ProcessingDbContextFactory : IDesignTimeDbContextFactory<ProcessingDbContext>
{
    public ProcessingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=processingservice;Username=postgres;Password=postgres";

        var schema =
            Environment.GetEnvironmentVariable("Database__Schema")
            ?? "processing";

        var enableSensitiveDataLogging =
            bool.TryParse(
                Environment.GetEnvironmentVariable("Database__EnableSensitiveDataLogging"),
                out var parsed)
            && parsed;

        var databaseOptions = new DatabaseOptions
        {
            ConnectionString = connectionString,
            Schema = schema,
            EnableSensitiveDataLogging = enableSensitiveDataLogging
        };

        var optionsBuilder = new DbContextOptionsBuilder<ProcessingDbContext>();

        optionsBuilder.UseNpgsql(
            databaseOptions.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

        if (databaseOptions.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        return new ProcessingDbContext(
            optionsBuilder.Options,
            Options.Create(databaseOptions));
    }
}