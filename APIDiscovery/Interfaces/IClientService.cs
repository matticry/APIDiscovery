using APIDiscovery.Models;

namespace APIDiscovery.Interfaces;

public interface IClientService 
{
    Task<IEnumerable<Client>> GetAllAsync(int? enterpriseId = null);
    Task<Client> GetByIdAsync(int id);
    Task<Client> GetByDniAsync(string dni);
    Task<IEnumerable<Enterprise>> GetClientEnterprisesAsync(int clientId);
    Task<Client> CreateAsync(Client entity, int enterpriseId);
    Task<bool> AssignClientToEnterpriseAsync(int clientId, int enterpriseId);
    Task<Client> UpdateAsync(int id, Client entity, int enterpriseId);
    Task<bool> RemoveClientFromEnterpriseAsync(int clientId, int enterpriseId);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalClientsActivesAsync(int enterpriseId);
    
}