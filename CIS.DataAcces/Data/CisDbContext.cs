using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Data;

public class CisDbContext : DbContext
{
    public CisDbContext(DbContextOptions<CisDbContext> options) : base(options) { }
}
