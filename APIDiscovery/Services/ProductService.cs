using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class ProductService : IProductService
{
    
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
        
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.id_pro == id);
    }

    public  async Task<Product> CreateAsync(Product entity)
    {
        var existingProductName = await _context.Products.FirstOrDefaultAsync(p => p.name_pro == entity.name_pro);
        if (existingProductName != null)
        {
            throw new BadRequestException("El nombre del producto ya existe.");
        }
        _context.Products.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Product> UpdateAsync(int id, Product entity)
    {
        var existingProductName = await _context.Products.FirstOrDefaultAsync(p => p.name_pro == entity.name_pro);
        if (existingProductName != null)
        {
            throw new BadRequestException("El nombre del producto ya existe.");
        }
        
        var product = await _context.Products.FirstOrDefaultAsync(p => p.id_pro == id);
        if (product == null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }
        product.name_pro = entity.name_pro;
        product.price_pro = entity.price_pro;
        product.amount_pro = entity.amount_pro;
        await _context.SaveChangesAsync();
        
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.id_pro == id);
        if (product == null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}