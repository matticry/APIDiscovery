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
    public DbSet<Category> Categories { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<Fare> Fares { get; set; }
    public DbSet<Tax> Taxes { get; set; }
    public DbSet<TariffArticles> TariffArticles { get; set; }

    
    
    

}