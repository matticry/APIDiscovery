using APIDiscovery.Core;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services.Commands;

public class CustomService
{
    protected ApplicationDbContext _context;

    public CustomService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<string>> GetResultRoleByNameAsync()
    {
        return await _context.Roles
            .Where(r => EF.Functions.Like(r.name_rol, "%Enfermero/a%") || EF.Functions.Like(r.name_rol, "%Familiar%"))
            .Select(r => r.name_rol)
            .ToListAsync();
        
        
    }
    
    
    
}