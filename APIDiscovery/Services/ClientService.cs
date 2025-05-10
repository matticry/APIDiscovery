using System.Text.Json;
using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;

    public ClientService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Client> GetByDniAsync(string dni)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.dni == dni);
        if (client == null)
        {
            throw new NotFoundException("Cliente con ese DNI no encontrado.");
        }
        return client;
    }
    
    public async Task<IEnumerable<Client>> GetAllAsync(int? enterpriseId = null)
    {
        if (!enterpriseId.HasValue) return await _context.Clients.ToListAsync();
        var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == enterpriseId.Value);
        if (enterprise == null)
        {
            throw new NotFoundException($"Empresa con ID {enterpriseId.Value} no encontrada.");
        }

        return await _context.EnterpriseClients
            .Where(ec => ec.enterprise_id == enterpriseId.Value)
            .Select(ec => ec.Client)
            .ToListAsync();

    }


    public async Task<Client> GetByIdAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == id);
        if (client == null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }
        return client;
    }

    

    public async Task<IEnumerable<Enterprise>> GetClientEnterprisesAsync(int clientId)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == clientId);
        if (client == null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        return await _context.EnterpriseClients
            .Where(ec => ec.client_id == clientId)
            .Select(ec => ec.Enterprise)
            .ToListAsync();
    }

    // Método modificado para crear cliente y asignarlo a una empresa
    public async Task<Client> CreateAsync(Client entity, int enterpriseId)
    {
        var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == enterpriseId);
        if (enterprise == null)
        {
            throw new NotFoundException($"Empresa con ID {enterpriseId} no encontrada.");
        }
        
        if (!string.IsNullOrEmpty(entity.dni) && !VerificaCedula(entity.dni.ToCharArray()))
        {
            throw new BadRequestException("El número de cédula proporcionado no es válido.");
        }
        
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.razon_social == entity.razon_social);
        if (existingClient != null)
        {
            throw new BadRequestException("Ya existe un cliente con la misma razón social.");
        }
    
        await ValidateDniClientAsync(entity.dni);
        await ValidateEmailWithHunter(entity.email);
        await ValidateEmailAsync(entity.email);
        await ValidatePhoneAsync(entity.phone);
    
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Clients.Add(entity);
            await _context.SaveChangesAsync();
            
            var enterpriseClient = new EnterpriseClient
            {
                enterprise_id = enterpriseId,
                client_id = entity.id_client
            };
            
            _context.EnterpriseClients.Add(enterpriseClient);
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            return entity;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // Método para asignar un cliente existente a una empresa
    public async Task<bool> AssignClientToEnterpriseAsync(int clientId, int enterpriseId)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == clientId);
        if (client == null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }
        
        var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == enterpriseId);
        if (enterprise == null)
        {
            throw new NotFoundException($"Empresa con ID {enterpriseId} no encontrada.");
        }
        
        // Verificar si la relación ya existe
        var exists = await _context.EnterpriseClients
            .AnyAsync(ec => ec.client_id == clientId && ec.enterprise_id == enterpriseId);
            
        if (exists)
        {
            throw new BadRequestException("El cliente ya está asignado a esta empresa.");
        }
        
        var enterpriseClient = new EnterpriseClient
        {
            enterprise_id = enterpriseId,
            client_id = clientId
        };
        
        _context.EnterpriseClients.Add(enterpriseClient);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    private static bool VerificaCedula(char[] validarCedula)
    {
        int aux = 0, par = 0, impar = 0, verifi;
        for (var i = 0; i < 9; i += 2)
        {
            aux = 2 * int.Parse(validarCedula[i].ToString());
            if (aux > 9)
                aux -= 9;
            par += aux;
        }
        for (var i = 1; i < 9; i += 2)
        {
            impar += int.Parse(validarCedula[i].ToString());
        }

        aux = par + impar;
        if (aux % 10 != 0)
        {
            verifi = 10 - (aux % 10);
        }
        else
            verifi = 0;
        return verifi == int.Parse(validarCedula[9].ToString());
    }
    
    // Métodos originales de validación
    private async Task ValidateEmailAsync(string email)
    {
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.email == email);
        if (existingClient != null)
        {
            throw new BadRequestException("Ya existe un cliente con el mismo correo electrónico.");
        }
    }
    
    private async Task ValidatePhoneAsync(string phone)
    {
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.phone == phone);
        if (existingClient != null)
        {
            throw new BadRequestException("Ya existe un cliente con el mismo teléfono.");
        }
    }

    private async Task ValidateDniClientAsync(string dni)
    {
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.dni == dni);
        if (existingClient != null)
        {
            throw new BadRequestException("Ya existe un cliente con el mismo DNI.");
        }
    }
    
    private async Task ValidateEmailWithHunter(string email)
    {
        var apiKey = Environment.GetEnvironmentVariable("HUNTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new BadRequestException("La clave de la API de Hunter no está configurada.");
        
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://api.hunter.io/v2/email-verifier?email={email}&api_key={apiKey}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
        
            if (root.TryGetProperty("data", out var data) && 
                data.TryGetProperty("status", out var status))
            {
                var emailStatus = status.GetString() ?? string.Empty;
            
                if (emailStatus != "valid")
                {
                    throw new BadRequestException("El correo electrónico proporcionado no es válido o no existe.");
                }
            }
        }
        else
        {
            throw new BadRequestException("No se pudo verificar el correo electrónico. Intente nuevamente más tarde.");
        }
    }

    public async Task<Client> UpdateAsync(int id, Client entity, int enterpriseId)
    {
        var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == enterpriseId);
        if (enterprise == null)
        {
            throw new NotFoundException($"Empresa con ID {enterpriseId} no encontrada.");
        }
        
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == id);
        if (client == null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }
        
        var relationship = await _context.EnterpriseClients
            .FirstOrDefaultAsync(ec => ec.client_id == id && ec.enterprise_id == enterpriseId);
        
        if (relationship == null)
        {
            throw new BadRequestException($"El cliente con ID {id} no está asociado a la empresa con ID {enterpriseId}.");
        }
        
        if (entity.dni != client.dni)
        {
            if (!string.IsNullOrEmpty(entity.dni) && !VerificaCedula(entity.dni.ToCharArray()))
            {
                throw new BadRequestException("El número de cédula proporcionado no es válido.");
            }
            await ValidateDniClientAsync(entity.dni);
        }
        
        if (entity.email != client.email)
        {
            await ValidateEmailWithHunter(entity.email);
            await ValidateEmailAsync(entity.email);
        }
        
        if (entity.phone != client.phone)
        {
            await ValidatePhoneAsync(entity.phone);
        }
        
        if (entity.razon_social != client.razon_social)
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.razon_social == entity.razon_social && c.id_client != id);
            if (existingClient != null)
            {
                throw new BadRequestException("Ya existe un cliente con la misma razón social.");
            }
        }
        
        client.razon_social = entity.razon_social;
        client.email = entity.email;
        client.address = entity.address;
        client.phone = entity.phone;
        client.info = entity.info;
        client.id_type_dni = entity.id_type_dni;
        client.dni = entity.dni;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return client;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Método para eliminar un cliente solo de una empresa específica
    public async Task<bool> RemoveClientFromEnterpriseAsync(int clientId, int enterpriseId)
    {
        var enterpriseClient = await _context.EnterpriseClients
            .FirstOrDefaultAsync(ec => ec.client_id == clientId && ec.enterprise_id == enterpriseId);
            
        if (enterpriseClient == null)
        {
            throw new NotFoundException("El cliente no está asignado a esta empresa.");
        }
        
        _context.EnterpriseClients.Remove(enterpriseClient);
        await _context.SaveChangesAsync();
        return true;
    }

    // Método modificado para eliminar completamente un cliente y todas sus relaciones
    public async Task<bool> DeleteAsync(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == id);
            if (client == null)
            {
                throw new NotFoundException("Cliente no encontrado.");
            }
            
            // Eliminar todas las relaciones del cliente con empresas
            var relations = await _context.EnterpriseClients
                .Where(ec => ec.client_id == id)
                .ToListAsync();
                
            _context.EnterpriseClients.RemoveRange(relations);
            
            // Eliminar el cliente
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> GetTotalClientsActivesAsync(int enterpriseId)
    {
        var enterprise = await _context.Enterprises.FirstOrDefaultAsync(e => e.id_en == enterpriseId);
        if (enterprise == null)
        {
            throw new NotFoundException($"Empresa con ID {enterpriseId} no encontrada.");
        }
        
        var status = await _context.EnterpriseClients.FirstOrDefaultAsync(s => s.status == 'A' && s.enterprise_id == enterpriseId);
        if (status == null)
        {
            throw new NotFoundException("No hay clientes activos en esta empresa.");
        }
        
        return await _context.EnterpriseClients
            .Where(ec => ec.enterprise_id == enterpriseId)
            .CountAsync();
    }
}