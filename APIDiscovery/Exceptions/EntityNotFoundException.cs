namespace APIDiscovery.Exceptions;

public class EntityNotFoundException : BaseException
{
    public EntityNotFoundException(string message) : base(message, "ENTITY_NOT_FOUND")
    {
    }
        
    public EntityNotFoundException(string entityName, int id) 
        : base($"La entidad {entityName} con ID {id} no fue encontrada", "ENTITY_NOT_FOUND")
    {
    }
        
    public EntityNotFoundException(string entityName, string code) 
        : base($"La entidad {entityName} con código {code} no fue encontrada", "ENTITY_NOT_FOUND")
    {
    }
}