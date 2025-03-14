using PassengerTransport.Clients;

namespace PassengerTransport.Vehicles
{
    public class VehicleManager
    {
        private readonly List<Vehicle> _vehicles = new();
        private readonly object _lock = new object();
        private readonly IGroundControlClient _gcClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleManager> _logger;
        
        public VehicleManager(
            IGroundControlClient gcClient,
            IServiceProvider serviceProvider,
            ILogger<VehicleManager> logger)
        {
            _gcClient = gcClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
            InitializeVehiclesAsync().Wait();
        }

        private async Task InitializeVehiclesAsync()
        {
            _logger.LogInformation("Starting vehicle initialization...");
            
            try
            {
                // 1. Генерируем ID для машинок
                var vehicleIds = Enumerable.Range(1, 5)
                    .Select(i => $"BUS-{i}")
                    .ToList();

                // 2. Запрашиваем узлы для инициализации
                var nodes = new List<string> { "CG-1", "CG-1", "CG-1", "CG-1", "CG-1" };

                // 3. Вызываем API инициализации
                var initResult = await _gcClient.InitVehiclesAsync(vehicleIds, nodes);
                
                if (!initResult)
                {
                    _logger.LogCritical("Vehicle initialization failed via API");
                    throw new System.Exception("API vehicle initialization failed");
                }

                foreach (var vehicleId in vehicleIds)
                {
                    var bus = _serviceProvider.GetRequiredService<PassengerBus>();
                    bus.Id = vehicleId;
                    bus.CurrentLocation = "CG-1"; 
                    bus.BaseLocation = "CG-1";
                    _vehicles.Add(bus);
                    
                    _logger.LogInformation("Created bus {Id} at {Location}", 
                        bus.Id, bus.CurrentLocation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize vehicles");
                throw;
            }
        }

    public Vehicle GetAvailableVehicle(string taskType)
    {
        lock (_lock)
        {
            var available = _vehicles
                .Where(v => !v.IsBusy)
                .OrderBy(v => Guid.NewGuid()) // Рандомизация выбора
                .FirstOrDefault();

            if (available != null)
            {
                available.IsBusy = true; // Помечаем как занятую сразу
                _logger.LogInformation("Vehicle {Id} selected for task {Type}", 
                    available.Id, taskType);
            }

            LogVehicleStates(taskType);
            return available;
        }
    }

        private void LogVehicleStates(string taskType)
        {
            _logger.LogInformation("Vehicle states for {TaskType}:", taskType);
            foreach (var v in _vehicles)
            {
                _logger.LogInformation(
                    "Vehicle {Id} | Type: {Type} | Busy: {Busy} | Location: {Location}",
                    v.Id,
                    v.GetType().Name,
                    v.IsBusy,
                    v.CurrentLocation);
            }
        }
    }
}