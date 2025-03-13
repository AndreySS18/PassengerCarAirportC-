using PassengerTransport.Clients;
using Newtonsoft.Json.Linq;

namespace PassengerTransport.Vehicles
{
    public class PassengerBus : Vehicle
    {
        public PassengerBus(
            IGroundControlClient gcClient,
            ILogger<PassengerBus> logger,
            IHandlingSupervisorClient hsClient) 
            : base(gcClient, logger, hsClient)
        {
            BaseLocation = "CG-1";
            CurrentLocation = BaseLocation;
        }

        public override async Task ExecuteTaskAsync(TaskMessage task)
        {
            try
            {
                IsBusy = true;
                SetTaskInfo(task.TaskId, GetPickupPoint(task.Details), task.Point);

                _logger.LogInformation("Starting task {TaskId} | Pickup: {From} | Destination: {To}", 
                    task.TaskId, PickupPoint, DestinationPoint);

                // 1. Move to pickup point
                await MoveToPoint(PickupPoint);
                
                // 2. Perform passenger operations
                //await PerformPassengerOperations(task.Details);
                
                // 3. Move to destination
                await MoveToPoint(DestinationPoint);

                // 4. Update task status
                await _hsClient.CompleteTaskAsync(task.TaskId);
                
                // 4. Return to base
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
            var path = await _gcClient.GetPathAsync(CurrentLocation, targetPoint);
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
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    _logger.LogInformation("Awaiting permission {From}->{To}", from, to);
                }
            } while (!permission);

            if (await _gcClient.SendMoveRequestAsync(Id, from, to))
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // Simulate movement
                
                if (await _gcClient.ConfirmArrivalAsync(Id, from, to))
                {
                    CurrentLocation = to;
                    _logger.LogInformation("Arrived at {Location}", to);
                }
            }
        }

        private string GetPickupPoint(JObject details)
        {
            return details?["Start"]?.ToString() ?? CurrentLocation;
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
                        await Task.Delay(TimeSpan.FromSeconds(5));
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

       /* private async Task PerformPassengerOperations(JObject details)
        {
            var passengerCount = details.Value<int>("passengers");
            var action = details.Value<string>("actionType");
            
            _logger.LogInformation("Performing {Action} for {Count} passengers", 
                action, passengerCount);

            await SimulatePassengerOperations(action, passengerCount);
        }*/

        private async Task SimulatePassengerOperations(string action, int count)
        {
            var delaySeconds = CalculateOperationDelay(count);
            _logger.LogInformation("Processing {Count} passengers ({Action})", count, action);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        private int CalculateOperationDelay(int passengerCount)
        {
            return Math.Max(2, passengerCount / 10);
        }

        private async Task ReturnToBase()
        {
            var basePath = await _gcClient.GetPathAsync(CurrentLocation, BaseLocation);
            if (basePath != null && basePath.Count > 0)
            {
                await MoveThroughPath(basePath);
                _logger.LogInformation("Successfully returned to base {Base}", BaseLocation);
            }
        }

    }
}