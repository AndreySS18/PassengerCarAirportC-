using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace PassengerTransport
{
    public class RabbitMQConsumer : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly ILogger<RabbitMQConsumer> _logger;

        public RabbitMQConsumer(
            string hostName, 
            string queueName,
            ILogger<RabbitMQConsumer> logger)
        {
            _queueName = queueName;
            _logger = logger;
            
            var factory = new ConnectionFactory();
            factory.Uri = new Uri(hostName);
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.QueueDeclare(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void StartConsuming(Action<TaskMessage> handler)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonConvert.DeserializeObject<TaskMessage>(
                        Encoding.UTF8.GetString(body));
                    _logger.LogInformation("information {message}", message);
                    
                    message.Details = ParseDetails(message.DetailsString);
                    handler(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            };

            _channel.BasicConsume(_queueName, true, consumer);
        }

        private JObject ParseDetails(string details)
        {
            try
            {
                return JObject.Parse(details);
            }
            catch
            {
                return new JObject();
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}