using System.Text;
using System.Text.Json;

namespace PassengerTransport.Clients
{
    public class BoardService : IBoardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PassengerService> _logger;
        private const int RetryDelayMs = 3000;
        private const int MaxRetries = 5;

        public BoardService(HttpClient httpClient, ILogger<PassengerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        public async Task<bool> PostPassengersOnBoardToBoard(IEnumerable<string> passengersIDs)
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
    }
}