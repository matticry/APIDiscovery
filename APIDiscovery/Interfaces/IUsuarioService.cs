using APIDiscovery.Models;

namespace APIDiscovery.Interfaces;

public interface IUsuarioService : ICrudService<Usuario>
{
    Task<Usuario> GetByEmailAsync(string email);
}