using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassengerTransport.Clients
{
    public class PassengerService : IPassengerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PassengerService> _logger;
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

private class FlightPassengersResponse
    {
        [JsonPropertyName("passengers")] 
        public List<string> Passengers { get; set; } = new List<string>();
    }
    }
}