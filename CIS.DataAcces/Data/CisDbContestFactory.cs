using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using CIS.DataAcces.Data;

namespace CIS.DataAcces.Data;
public class CisDbContextFactory : IDesignTimeDbContextFactory<CisDbContext>
{
    public CisDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CisDbContext>();

        optionsBuilder.UseMySql(
            "Server=127.0.0.1;Port=3307;Database=sd3;User=SOME_USER;Password=SOME_PASSWORD;AllowUserVariables=true;AllowZeroDateTime=true",
            new MySqlServerVersion(new Version(8, 0, 0))
        );

        return new CisDbContext(optionsBuilder.Options);
    }
}