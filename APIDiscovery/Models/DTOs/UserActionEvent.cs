namespace APIDiscovery.Models.DTOs;

public class UserActionEvent
{
    public string Action { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; }
    public string Dni { get; set; }
    
    // Constructor para facilitar la creación
    public UserActionEvent()
    {
        CreatedAt = DateTime.Now;
    }
}