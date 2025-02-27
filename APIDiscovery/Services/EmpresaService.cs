using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class EmpresaService : IEmpresaService
{
    protected ApplicationDbContext _context;
    
    public EmpresaService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Empresa>> GetAllAsync()
    {
        return await _context.Empresas.ToListAsync();
        
    }

    public async Task<Empresa> GetByIdAsync(int id)
    {
        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.id_empresa == id);
        if (empresa == null)
        {
            throw new NotFoundException("Empresa no encontrada.");
        }
        return empresa;
    }

    public async Task<Empresa> CreateAsync(Empresa entity)
    {
        if (string.IsNullOrEmpty(entity.name_empresa) || string.IsNullOrEmpty(entity.ruc_empresa))
        {
            throw new BadRequestException("Los campos nombre de la empresa y RUC son obligatorios.");
            
        }

        _context.Empresas.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
        
    }

    public async Task<Empresa> UpdateAsync(int id, Empresa entity)
    {
        var empresa = _context.Empresas.FirstOrDefault(e => e.id_empresa == id);
        if (empresa == null)
        {
            throw new NotFoundException("Empresa no encontrada.");
        }
        empresa.name_empresa = entity.name_empresa;
        empresa.ruc_empresa = entity.ruc_empresa;
        await _context.SaveChangesAsync();
        return empresa;
        
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.id_empresa == id);
        if (empresa == null)
        {
            throw new NotFoundException("Empresa no encontrada.");
        }
        _context.Empresas.Remove(empresa);
        await _context.SaveChangesAsync();
        return true;
    }
}