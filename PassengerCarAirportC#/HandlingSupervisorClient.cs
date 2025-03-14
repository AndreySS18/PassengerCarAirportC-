using System.Text;
using System.Text.Json;


namespace PassengerTransport.Clients
{
    public class HandlingSupervisorClient : IHandlingSupervisorClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HandlingSupervisorClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public HandlingSupervisorClient(HttpClient httpClient, ILogger<HandlingSupervisorClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> AssignTaskAsync(string vehicleId, string taskId)
        {
            try
            {
                var request = new { carId = vehicleId.ToString() };
                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync($"/v1/tasks/{taskId}/assign", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Assign task failed. Status: {StatusCode}", response.StatusCode);
                    return false;
                }

                _logger.LogInformation("Task {TaskId} assigned to vehicle {VehicleId}", taskId, vehicleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> CompleteTaskAsync(string taskId)
        {
            try
            {
                var response = await _httpClient.PutAsync(
                    $"/v1/tasks/{taskId}/complete", 
                    new StringContent(string.Empty));
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Complete task failed. Status: {StatusCode}", response.StatusCode);
                    return false;
                }

                _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId}", taskId);
                return false;
            }
        }
    }
}