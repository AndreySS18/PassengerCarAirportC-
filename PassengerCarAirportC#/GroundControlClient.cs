using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace PassengerTransport.Clients
{
    public class GroundControlClient : IGroundControlClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroundControlClient> _logger;

        public GroundControlClient(HttpClient httpClient, ILogger<GroundControlClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<List<string>> GetPathAsync(string from, string to)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v1/map/path?from={from}&to={to}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<PathResponse>(content);
                
                return result?.Path ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Path request failed: {From} -> {To}", from, to);
                throw;
            }
        }

        public async Task<bool> RequestMovePermissionAsync(string vehicleId, string from, string to)
        {
            try
            {
                var requestUrl = $"/v1/vehicles/move_permission" +
                    $"?guid={vehicleId}" +
                    $"&vehicleType=car" +
                    $"&from={Uri.EscapeDataString(from)}" +
                    $"&to={Uri.EscapeDataString(to)}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseBody = await response.Content.ReadAsStringAsync();

                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Move permission request failed. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseBody);
                    return false;
                }

                var result = JsonSerializer.Deserialize<MovePermissionResponse>(responseBody);
                return result?.Allowed ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Move permission request error");
                return false;
            }
        }

        public async Task<bool> SendMoveRequestAsync(string vehicleId, string from, string to)
        {
            try
            {
                var request = new 
                {
                    guid = vehicleId,
                    vehicleType = "car",
                    from,
                    to
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/v1/vehicles/move", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning("Invalid move request for {VehicleId}", vehicleId);
                    return false;
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Move request failed");
                return false;
            }
        }

        public async Task<bool> ConfirmArrivalAsync(string vehicleId, string from, string to)
        {
            try
            {
                var request = new 
                {
                    guid = vehicleId,
                    vehicleType = "car",
                    from,
                    to
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/v1/vehicles/arrived", content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning("Invalid arrival confirmation for {VehicleId}", vehicleId);
                    return false;
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arrival confirmation failed");
                return false;
            }
        }
        public async Task<bool> InitVehiclesAsync(IEnumerable<string> vehicleIds, IEnumerable<string> nodes)
        {
            try
            {
                var request = new 
                {
                    vehicles = vehicleIds,
                    nodes = nodes
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/v1/vehicles/init", content);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vehicle initialization failed");
                return false;
            }
        }
    }
    public class MovePermissionResponse
    {
        [JsonPropertyName("guid")] 
        public string Guid { get; set; }
        
        [JsonPropertyName("from")] 
        public string? From { get; set; }
        
        [JsonPropertyName("to")] 
        public string? To { get; set; }
        
        [JsonPropertyName("allowed")] 
        public bool Allowed { get; set; }
    }
    public class PathResponse
    {
        [JsonPropertyName("path")]
        public List<string> Path { get; set; } = new List<string>();
    }
}