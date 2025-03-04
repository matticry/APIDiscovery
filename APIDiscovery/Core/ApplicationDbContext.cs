﻿using APIDiscovery.Models;
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

}