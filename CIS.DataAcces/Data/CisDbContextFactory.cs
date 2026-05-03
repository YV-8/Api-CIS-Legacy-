using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using CIS.DataAcces.Data;

namespace CIS.DataAcces.Data;

public class CisDbContextFactory : IDesignTimeDbContextFactory<CisDbContext>
{
    public CisDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Server=127.0.0.1;Port=3307;Database=sd3;User=root;Password=root;AllowUserVariables=true;AllowZeroDateTime=true";

        var optionsBuilder = new DbContextOptionsBuilder<CisDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 0))
        );

        return new CisDbContext(optionsBuilder.Options);
    }
}