
namespace PassengerTransport.Clients
{
    public interface IPassengerService
    {
        Task<List<string>> GetPassengersForFlightAsync(string flightId);
        Task<bool> PostPassengersOnBoard(IEnumerable<string> passengersIDs);
    }
}