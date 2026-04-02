using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Data;

public class CisDbContext : DbContext
{
    public CisDbContext(DbContextOptions<CisDbContext> options) : base(options) { }
    public DbSet<Topic> Topics { get; set; }
}
