using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Core;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Token> Tokens { get; set; } 
    public DbSet<Enterprise> Enterprises { get; set; }
    public DbSet<EnterpriseUser> EnterpriseUsers { get; set; }
    public DbSet<Branch> Branches { get; set; }
    
    
    

}