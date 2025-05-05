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
    
    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _context.Clients.ToListAsync();
    }

    public async Task<Client> GetByIdAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == id);
        if (client == null)
        {
            throw new NotFoundException("Client not found.");
        }
        return client;
    }

    public async Task<Client> CreateAsync(Client entity)
    {
        
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
    
        _context.Clients.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    
    
    private static bool VerificaCedula(char[] validarCedula)
    {
        int aux = 0, par = 0, impar = 0, verifi;
        for (int i = 0; i < 9; i += 2)
        {
            aux = 2 * int.Parse(validarCedula[i].ToString());
            if (aux > 9)
                aux -= 9;
            par += aux;
        }
        for (int i = 1; i < 9; i += 2)
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
        if (verifi == int.Parse(validarCedula[9].ToString()))
            return true;
        else
            return false;
    }
    
    private async Task ValidateEmailAsync(string email)
    {
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.email == email);
        if (existingClient != null)
        {
            throw new BadRequestException("Ya existe un cliente con el mismo correo electrónica.");
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
            throw new BadRequestException("La clave de la API de Hunter no está configurada.");
        
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
                string emailStatus = status.GetString() ?? string.Empty;
            
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
    

    public async Task<Client> UpdateAsync(int id, Client entity)
    {
        var client = _context.Clients.FirstOrDefault(c => c.id_client == id);
        if (client == null)
        {
            throw new NotFoundException("Client not found.");
        }
        client.address = entity.address;
        client.phone = entity.phone;
        client.info = entity.info;
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.id_client == id);
        if (client == null)
        {
            throw new NotFoundException("Client not found.");
        }
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return true;
    }
}