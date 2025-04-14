using System.Text;
using APIDiscovery.Models.DTOs;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IModel = RabbitMQ.Client.IModel;

namespace APIDiscovery.Services;

public class UserActionConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserActionConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public UserActionConsumerService(
        IConfiguration configuration, 
        ILogger<UserActionConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        try 
        {
            _logger.LogInformation("Iniciando servicio consumidor de RabbitMQ...");
            
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "192.168.1.12",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };
            
            _logger.LogInformation($"Configuración RabbitMQ: Host={factory.HostName}, Port={factory.Port}");
            
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("Conexión establecida con RabbitMQ");
            
            _channel.ExchangeDeclare(
                exchange: "user_actions_exchange", 
                type: ExchangeType.Direct,
                durable: true);
            
            _logger.LogInformation("Exchange 'user_actions_exchange' declarado");
            
            _channel.QueueDeclare(
                queue: "user_actions_queue",
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            _logger.LogInformation("Cola 'user_actions_queue' declarada");
            
            _channel.QueueBind(
                queue: "user_actions_queue",
                exchange: "user_actions_exchange",
                routingKey: "user.action");
            
            _logger.LogInformation("Cola vinculada al exchange con routing key 'user.action'");
            
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            
            _logger.LogInformation("Servicio consumidor de RabbitMQ iniciado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al iniciar el servicio consumidor de RabbitMQ: {ex.Message}");
            throw; // Re-lanzar la excepción para que el servicio no se inicie si hay problemas
        }
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando consumo de mensajes de RabbitMQ...");
        
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation($"Mensaje recibido: {message}");
                
                var userAction = JsonConvert.DeserializeObject<UserActionEvent>(message);
                
                if (userAction != null)
                {
                    SaveToDatabase(userAction);
                    
                    // Confirmar que el mensaje se procesó correctamente
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    
                    _logger.LogInformation($"Mensaje procesado y guardado en base de datos");
                }
                else
                {
                    _logger.LogWarning("El mensaje no pudo ser deserializado correctamente");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al procesar mensaje: {ex.Message}");
                // Rechazar el mensaje en caso de error para que vuelva a la cola
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };
        
        _channel.BasicConsume(
            queue: "user_actions_queue",
            autoAck: false, // Importante: no confirmar automáticamente
            consumer: consumer);
        
        _logger.LogInformation("Consumidor registrado y escuchando mensajes");
        
        return Task.CompletedTask;
    }
    
    private void SaveToDatabase(UserActionEvent userAction)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
    
        _logger.LogInformation($"Guardando en base de datos: {JsonConvert.SerializeObject(userAction)}");
    
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
        
            _logger.LogInformation("Conexión a base de datos abierta");
        
            // Asegurar que Dni tenga un valor válido
            string dni = string.IsNullOrEmpty(userAction.Dni) ? "1755386099" : userAction.Dni;
        
            var cmd = new SqlCommand(@"
            INSERT INTO tbl_report (action_re, created_at_re, user_re, dni_re, status_re)
            VALUES (@action, @createdAt, @user, @dni, 'A')", connection);
        
            cmd.Parameters.AddWithValue("@action", userAction.Action);
            cmd.Parameters.AddWithValue("@createdAt", userAction.CreatedAt);
            cmd.Parameters.AddWithValue("@user", userAction.Username);
            cmd.Parameters.AddWithValue("@dni", dni);  // Usar el valor ya validado
        
            var rowsAffected = cmd.ExecuteNonQuery();
        
            _logger.LogInformation($"Filas afectadas: {rowsAffected}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al guardar en base de datos: {ex.Message}");
            throw;
        }
    }
    
    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}