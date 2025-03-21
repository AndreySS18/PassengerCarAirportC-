using PassengerTransport.Clients;

namespace PassengerTransport.Vehicles
{
    public abstract class Vehicle
    {
        protected readonly IGroundControlClient _gcClient;
        protected readonly IHandlingSupervisorClient _hsClient;
        protected readonly IPassengerService _passengerService;
        protected readonly IBoardService _boardService;
        protected readonly ILogger _logger;
        public string Id { get; set;} 
        public string BaseLocation { get; set; }
        public string CurrentLocation { get; set; }
        public bool IsBusy { get; set; }
        public string CurrentTaskId { get; protected set; }
        public string DestinationPoint { get; protected set; }
        public string PickupPoint { get; protected set; }

        protected Vehicle(
            IGroundControlClient gcClient,
            ILogger logger,
            IHandlingSupervisorClient hsClient,
            IPassengerService passengerService,
            IBoardService boardService)
        {
            _gcClient = gcClient;
            _logger = logger;
            _hsClient = hsClient;
            _passengerService = passengerService;
            _boardService = boardService;
        }

        public abstract Task ExecuteTaskAsync(TaskMessage task);
        
        public virtual void SetTaskInfo(string taskId, string pickupPoint, string destinationPoint)
        {
            CurrentTaskId = taskId;
            PickupPoint = pickupPoint;
            DestinationPoint = destinationPoint;
        }
    }
}