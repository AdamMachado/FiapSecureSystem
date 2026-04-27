using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using ReportService.Infrastructure.Configuration.Options;

namespace ReportService.Infrastructure.Persistence.Context;

public sealed class ReportDbContextFactory : IDesignTimeDbContextFactory<ReportDbContext>
{
    public ReportDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=fiap_secure_systems;Username=postgres;Password=postgres";

        var schema =
            Environment.GetEnvironmentVariable("Database__Schema")
            ?? "report";

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

        var optionsBuilder = new DbContextOptionsBuilder<ReportDbContext>();

        optionsBuilder.UseNpgsql(
            databaseOptions.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

        if (databaseOptions.EnableSensitiveDataLogging)
            optionsBuilder.EnableSensitiveDataLogging();

        return new ReportDbContext(optionsBuilder.Options, Options.Create(databaseOptions));
    }
}