using System.Collections.Generic;
using System.Threading.Tasks;

namespace PassengerTransport.Clients
{
    public interface IPassengerService
    {
        Task<List<string>> GetPassengersForFlightAsync(string flightId);
    }
}