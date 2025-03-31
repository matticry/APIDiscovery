using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Core;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Token> Tokens { get; set; } 
    public DbSet<Children> Children { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<VentaProductoUsuario> VentaProductoUsuario { get; set; }

}