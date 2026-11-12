using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
/// Creates the database context for EF command-line tools
/// without starting the API.
/// </summary>
public sealed class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Used only to construct the model for migration generation.
            // Database operations require a real connection configuration.
            connectionString =
                "server=127.0.0.1;database=skycrop_design;" +
                "user=design_only;password=unused;";
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(Environment.ExpandEnvironmentVariables(connectionString))
            .Options;

        return new AppDbContext(options);
    }
}