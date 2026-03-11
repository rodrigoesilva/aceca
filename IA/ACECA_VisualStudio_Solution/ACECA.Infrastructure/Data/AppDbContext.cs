
using Microsoft.EntityFrameworkCore;
using ACECA.Core.Entities;

namespace ACECA.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
}
