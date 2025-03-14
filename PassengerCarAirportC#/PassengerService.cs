using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerTransport.Clients
{
    public class PassengerService : IPassengerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PassengerService> _logger;
        private const int RetryDelayMs = 3000;
        private const int MaxRetries = 5;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PassengerService(HttpClient httpClient, ILogger<PassengerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<string>> GetPassengersForFlightAsync(string flightId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v1/passengersId/flight/{flightId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FlightPassengersResponse>(content, _options);
                
                return result?.Passengers ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get passengers for flight {FlightId}", flightId);
                return new List<string>();
            }
        }
        
        public async Task<bool> PostPassengersOnBoard(IEnumerable<string> passengersIDs)
        {
            int attempt = 0;
            while (true)
            {
                try
                {

                    var content = new StringContent(
                        JsonSerializer.Serialize(passengersIDs),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync("/v1/passengers/board", content);
                    
                    if (response.IsSuccessStatusCode)
                        return true;

                    _logger.LogWarning("Send list of passengers failed (attempt {Attempt})", attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Send list of passengers error (attempt {Attempt})", attempt);
                }

                if (attempt++ >= MaxRetries) return false;
                await Task.Delay(RetryDelayMs);
            }
        }

private class FlightPassengersResponse
    {
        [JsonPropertyName("passengers")] 
        public List<string> Passengers { get; set; } = new List<string>();
    }
    }
}