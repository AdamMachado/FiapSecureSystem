using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.Persistence.Context;

public sealed class UploadDbContextFactory : IDesignTimeDbContextFactory<UploadDbContext>
{
    public UploadDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=uploadservice;Username=postgres;Password=postgres";

        var schema =
            Environment.GetEnvironmentVariable("Database__Schema")
            ?? "upload";

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

        var optionsBuilder = new DbContextOptionsBuilder<UploadDbContext>();

        optionsBuilder.UseNpgsql(
            databaseOptions.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

        if (databaseOptions.EnableSensitiveDataLogging)
            optionsBuilder.EnableSensitiveDataLogging();

        return new UploadDbContext(optionsBuilder.Options, Options.Create(databaseOptions));
    }
}