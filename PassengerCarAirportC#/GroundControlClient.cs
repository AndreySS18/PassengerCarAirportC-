using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PassengerTransport.Clients
{
    public class GroundControlClient : IGroundControlClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroundControlClient> _logger;
        private const int RetryDelayMs = 3000;
        private const int MaxRetries = 5;

        public GroundControlClient(HttpClient httpClient, ILogger<GroundControlClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<string>> GetPathAsync(string from, string to)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/v1/map/path?from={from}&to={to}");
                    response.EnsureSuccessStatusCode();
                    
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PathResponse>(content);
                    
                    if (result?.Path?.Count > 0)
                        return result.Path;

                    _logger.LogWarning("Empty path received, retrying...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Path request failed: {From} -> {To} (attempt {Attempt})", from, to, attempt);
                }

                if (attempt++ >= MaxRetries) throw new Exception("Max retries exceeded");
                await Task.Delay(RetryDelayMs);
            }
        }

        public async Task<bool> RequestMovePermissionAsync(string vehicleId, string from, string to)
        {
            int attempt = 0;
            while (true)
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

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<MovePermissionResponse>(responseBody);
                        if (result?.Allowed ?? false) 
                            return true;
                    }

                    _logger.LogWarning("Move permission denied (attempt {Attempt})", attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Move permission request error (attempt {Attempt})", attempt);
                }

                if (attempt++ >= MaxRetries) return false;
                await Task.Delay(RetryDelayMs);
            }
        }

        public async Task<bool> SendMoveRequestAsync(string vehicleId, string from, string to)
        {
            int attempt = 0;
            while (true)
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
                    
                    if (response.IsSuccessStatusCode)
                        return true;

                    _logger.LogWarning("Move request failed with status {StatusCode} (attempt {Attempt})", 
                        response.StatusCode, attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Move request failed (attempt {Attempt})", attempt);
                }

                if (attempt++ >= MaxRetries) return false;
                await Task.Delay(RetryDelayMs);
            }
        }

        public async Task<bool> ConfirmArrivalAsync(string vehicleId, string from, string to)
        {
            int attempt = 0;
            while (true)
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
                    
                    if (response.IsSuccessStatusCode)
                        return true;

                    _logger.LogWarning("Arrival confirmation failed (attempt {Attempt})", attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Arrival confirmation error (attempt {Attempt})", attempt);
                }

                if (attempt++ >= MaxRetries) return false;
                await Task.Delay(RetryDelayMs);
            }
        }

        public async Task<bool> InitVehiclesAsync(IEnumerable<string> vehicleIds, IEnumerable<string> nodes)
        {
            int attempt = 0;
            while (true)
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
                    
                    if (response.IsSuccessStatusCode)
                        return true;

                    _logger.LogWarning("Init vehicles failed (attempt {Attempt})", attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Vehicle initialization error (attempt {Attempt})", attempt);
                }

                if (attempt++ >= MaxRetries) return false;
                await Task.Delay(RetryDelayMs);
            }
        }
    }

    public class MovePermissionResponse
    {
        [JsonPropertyName("guid")] 
        public string Guid { get; set; }
        
        [JsonPropertyName("from")] 
        public string From { get; set; }
        
        [JsonPropertyName("to")] 
        public string To { get; set; }
        
        [JsonPropertyName("allowed")] 
        public bool Allowed { get; set; }
    }

    public class PathResponse
    {
        [JsonPropertyName("path")]
        public List<string> Path { get; set; } = new List<string>();
    }
}