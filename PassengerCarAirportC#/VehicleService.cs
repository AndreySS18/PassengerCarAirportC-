using PassengerTransport.Clients;
using PassengerTransport.Vehicles;


namespace PassengerTransport.Services
{
    public class VehicleService : BackgroundService
    {
        private readonly RabbitMQConsumer _consumer;
        private readonly ILogger<VehicleService> _logger;
        private readonly VehicleManager _vehicleManager;
        private readonly IHandlingSupervisorClient _hsClient;

        public VehicleService(
            RabbitMQConsumer consumer,
            ILogger<VehicleService> logger,
            VehicleManager vehicleManager,
            IHandlingSupervisorClient hsClient)
        {
            _consumer = consumer;
            _logger = logger;
            _vehicleManager = vehicleManager;
            _hsClient = hsClient;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting vehicle service");
            _consumer.StartConsuming(HandleTask);
            return Task.CompletedTask;
        }

        private async void HandleTask(TaskMessage task)
        {
            try
            {
                _logger.LogInformation("Processing task {TaskId} of type {TaskType}", 
                    task.TaskId, task.TaskType);

                // 1. Найти свободное транспортное средство
                var vehicle = _vehicleManager.GetAvailableVehicle(task.TaskType);
                if (vehicle == null)
                {
                    _logger.LogWarning("No available vehicles for task {TaskId}", task.TaskId);
                    return;
                }

                // 2. Назначить задачу
                await _hsClient.AssignTaskAsync(vehicle.Id, task.TaskId);

                // 3. Выполнить задачу
                await vehicle.ExecuteTaskAsync(task);

                // 4. Подтвердить выполнение
                await _hsClient.CompleteTaskAsync(task.TaskId);

                _logger.LogInformation("Task {TaskId} completed successfully", task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process task {TaskId}", task.TaskId);
            }
        }
    }
}