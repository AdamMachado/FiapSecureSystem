using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        var optionsBuilder = new DbContextOptionsBuilder<UploadDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema));

        return new UploadDbContext(optionsBuilder.Options);
    }
}