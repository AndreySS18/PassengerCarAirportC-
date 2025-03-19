using PassengerTransport.Clients;

namespace PassengerTransport.Vehicles
{
    public class PassengerBus : Vehicle
    {
        public readonly List<string> _passengers = new List<string>();
        public int Capacity { get; } = 170; 
        public int CurrentPassengers => _passengers.Count;
        public PassengerBus(
            IGroundControlClient gcClient,
            ILogger<PassengerBus> logger,
            IHandlingSupervisorClient hsClient,
            IPassengerService passengerService,
            IBoardService boardService) 
            : base(gcClient, logger, hsClient, passengerService, boardService)
        {
            BaseLocation = "CG-1";
            CurrentLocation = BaseLocation;
        }

        public override async Task ExecuteTaskAsync(TaskMessage task)
        {
            try
            {
                IsBusy = true;
                if (task.TaskType == "deliverPassengers")
                SetTaskInfo(task.TaskId, GetPickupPoint(task.Details), task.Point);
                else if (task.TaskType == "pickUpPassengers")
                SetTaskInfo(task.TaskId, task.Point, GetPickupPointFromPlane(task.Details)); 

                _logger.LogInformation("Starting task {TaskId} | Pickup: {From} | Destination: {To}", 
                    task.TaskId, PickupPoint, DestinationPoint);

                    // 1. Move to pickup point
                    await MoveToPoint(PickupPoint);
                    
                    // 2. Perform passenger operations
                    if (task.TaskType == "deliverPassengers")
                    await PerformPassengerOperations(task.FlightId);
                    else if (task.TaskType == "pickUpPassengers")
                    {
                        if (GetPassengersCount(task.Details) < Capacity)
                        _logger.LogInformation("Passengers were successfully picked up from the {FlightId} | in the amount of {PassengersCount}", 
                        task.FlightId, GetPassengersCount(task.Details));
                    }
                    
                    // 3. Move to destination
                    await MoveToPoint(DestinationPoint);

                    // 4. Update task status
                    await _hsClient.CompleteTaskAsync(task.TaskId);

                    // 5. Send List of passengers
                    if (task.TaskType == "deliverPassengers")
                    {
                        var initResult = await _passengerService.PostPassengersOnBoard(_passengers);
                        if (initResult)
                            _logger.LogInformation("Successful send list of passengers to Passenger");
                    }

                    // 6. Clear List of passengers
                    _passengers.Clear();

                    // 7. Return to base
                    await ReturnToBase();
            }
            finally
            {
                IsBusy = false;
                ClearTaskInfo();
            }
        }

        private async Task MoveToPoint(string targetPoint)
        {
            var path = await _gcClient.GetPathAsync(CurrentLocation, targetPoint, Id);
            if (path == null || path.Count == 0)
            {
                _logger.LogError("No path to {Point}", targetPoint);
                return;
            }

            for (int i = 0; i < path.Count-1; i++)
            {
                await ProcessMovementSegment(path[i], path[i+1]);
            }
        }

        private async Task ProcessMovementSegment(string from, string to)
        {
            bool permission;
            do
            {
                permission = await _gcClient.RequestMovePermissionAsync(Id, from, to);
                if (!permission)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    _logger.LogInformation("Awaiting permission {From}->{To}", from, to);
                }
            } while (!permission);

            if (await _gcClient.SendMoveRequestAsync(Id, from, to))
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); 
                
                if (await _gcClient.ConfirmArrivalAsync(Id, from, to))
                {
                    CurrentLocation = to;
                    _logger.LogInformation("Arrived at {Location}", to);
                }
            }
        }

        private string GetPickupPoint(TaskDetails details)
        {
            return details.Gate ?? CurrentLocation;
        }

        private string GetPickupPointFromPlane(TaskDetails details)
        {
            return details.TakeTo ?? CurrentLocation;
        }

        private int GetPassengersCount(TaskDetails details)
        {
            return details.PassengersCount;
        }

        private void ClearTaskInfo()
        {
            CurrentTaskId = null;
            PickupPoint = null;
            DestinationPoint = null;
        }

        private async Task MoveThroughPath(List<string> path)
        {
            if (path == null || path.Count == 0)
            {
                _logger.LogError("Invalid path provided");
                return;
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                var current = path[i];
                var next = path[i + 1];

                bool permission;
                do 
                {
                    permission = await _gcClient.RequestMovePermissionAsync(Id, current, next);
                    if (!permission)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        _logger.LogInformation("Retrying move permission {Current}->{Next}", current, next);
                    }
                } while (!permission);

                if (await _gcClient.SendMoveRequestAsync(Id, current, next))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    
                    if (await _gcClient.ConfirmArrivalAsync(Id, current, next))
                    {
                        CurrentLocation = next;
                        _logger.LogInformation("Moved to {Location}", next);
                    }
                }
            }
        }

        private async Task PerformPassengerOperations(string FlightId)
        {
            _passengers.Clear(); // Очищаем перед новой задачей

            var flightId = FlightId.ToString();
            var passengerIds = await _passengerService.GetPassengersForFlightAsync(flightId);
            
            _logger.LogInformation("Loading {Total} passengers (capacity: {Capacity})", 
                passengerIds.Count, Capacity);

            foreach (var passengerId in passengerIds)
            {
                if (CurrentPassengers >= Capacity)
                {
                    _logger.LogWarning("Capacity reached ({Capacity}), cannot load more passengers", Capacity);
                    break;
                }
                
                _passengers.Add(passengerId);
            }

            _logger.LogInformation("Successfully loaded {Count} passengers", CurrentPassengers);
        }

        private async Task ReturnToBase()
        {
            var basePath = await _gcClient.GetPathAsync(CurrentLocation, BaseLocation, Id);
            if (basePath != null && basePath.Count > 0)
            {
                await MoveThroughPath(basePath);
                _logger.LogInformation("Successfully returned to base {Base}", BaseLocation);
            }
        }

    }
}