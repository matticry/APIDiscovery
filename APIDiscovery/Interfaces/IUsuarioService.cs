using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario> GetByIdAsync(int id);
    Task<Usuario> CreateAsync(UsuarioRequest usuarioRequest);
    Task<Usuario> UpdateAsync(int id, UsuarioRequest usuario);
    Task<bool> DeleteAsync(int id);
    Task<Usuario> GetByEmailAsync(string email);
}