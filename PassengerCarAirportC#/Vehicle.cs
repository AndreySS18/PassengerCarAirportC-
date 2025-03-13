using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PassengerTransport.Clients;

namespace PassengerTransport.Vehicles
{
    public abstract class Vehicle
    {
        protected readonly IGroundControlClient _gcClient;
        protected readonly IHandlingSupervisorClient _hsClient;
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
            IHandlingSupervisorClient hsClient)
        {
            _gcClient = gcClient;
            _logger = logger;
            _hsClient = hsClient;
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