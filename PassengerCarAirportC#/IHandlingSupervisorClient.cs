using System;
using System.Threading.Tasks;

namespace PassengerTransport.Clients
{
    public interface IHandlingSupervisorClient
{
    Task<bool> AssignTaskAsync(string vehicleId, string taskId);
    Task<bool> CompleteTaskAsync(string taskId);
}
}