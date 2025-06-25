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
    public DbSet<Tax> Taxes { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<Fare> Fares { get; set; }
    public DbSet<TariffArticle> TariffArticles { get; set; }
    public DbSet<EmissionPoint> EmissionPoints { get; set; }
    public DbSet<Sequence> Sequences { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<InvoicePayment> InvoicePayments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<TypeDni> TypeDnis { get; set; }
    public DbSet<EnterpriseClient> EnterpriseClients { get; set; }
    public DbSet<CreditNote> CreditNotes { get; set; }        // ← AGREGAR
    public DbSet<CreditNoteDetail> CreditNoteDetails { get; set; }



    
    
    

}