using System.Text;
using APIDiscovery.Models.DTOs;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace APIDiscovery.Services;

public class RabbitMQService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly bool _isConnected;
    
    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "192.168.1.8",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };
            
            // Crear una conexión persistente
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Configurar exchange y cola
            _channel.ExchangeDeclare(
                exchange: "user_actions_exchange", 
                type: ExchangeType.Direct,
                durable: true);
            
            _channel.QueueDeclare(
                queue: "user_actions_queue",
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            _channel.QueueBind(
                queue: "user_actions_queue",
                exchange: "user_actions_exchange",
                routingKey: "user.action");
            
            _isConnected = true;
            _logger.LogInformation("Conexión establecida con RabbitMQ");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError($"Error al conectar con RabbitMQ: {ex.Message}");
        }
    }
    
    public void PublishUserAction(UserActionEvent userAction)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("No se puede publicar el mensaje porque no hay conexión a RabbitMQ");
            return;
        }
        
        try
        {
            // Agregar la fecha si no está establecida
            if (userAction.CreatedAt == default)
            {
                userAction.CreatedAt = DateTime.Now;
            }
            
            // Convertir el mensaje a JSON y enviarlo
            var json = JsonConvert.SerializeObject(userAction);
            var body = Encoding.UTF8.GetBytes(json);
            
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            
            _channel.BasicPublish(
                exchange: "user_actions_exchange",
                routingKey: "user.action",
                basicProperties: properties,
                body: body);
            
            _logger.LogInformation($"Mensaje enviado a RabbitMQ: {json}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al publicar mensaje en RabbitMQ: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}