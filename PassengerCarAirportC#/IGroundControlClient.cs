namespace PassengerTransport.Clients
{
    public interface IGroundControlClient
    {
        Task<List<string>> GetPathAsync(string from, string to);
        Task<bool> RequestMovePermissionAsync(string vehicleId, string from, string to);
        Task<bool> SendMoveRequestAsync(string vehicleId, string from, string to);
        Task<bool> ConfirmArrivalAsync(string vehicleId, string from, string to);
        Task<bool> InitVehiclesAsync(IEnumerable<string> vehicleIds, IEnumerable<string> nodes);
    }
}