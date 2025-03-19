
namespace PassengerTransport.Clients
{
    public interface IBoardService
    {
        Task<bool> PostPassengersOnBoardToBoard(IEnumerable<string> passengersIDs);
    }
}