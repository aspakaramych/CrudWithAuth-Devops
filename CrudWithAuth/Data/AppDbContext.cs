using CrudWithAuth.Entity;
using Microsoft.EntityFrameworkCore;

namespace CrudWithAuth.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users { get; set; }
}