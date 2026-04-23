using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.Persistence.Context;

public sealed class UploadDbContextFactory : IDesignTimeDbContextFactory<UploadDbContext>
{
    public UploadDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? throw new InvalidOperationException("Database configuration not found.");

        var optionsBuilder = new DbContextOptionsBuilder<UploadDbContext>();

        optionsBuilder.UseNpgsql(
            databaseOptions.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

        var databaseOptionsWrapper = Options.Create(databaseOptions);

        return new UploadDbContext(optionsBuilder.Options, databaseOptionsWrapper);
    }
}