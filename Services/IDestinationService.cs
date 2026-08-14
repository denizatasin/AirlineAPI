using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface IDestinationService
{
    Task<List<Destination>>GetAllDestinationsAsync();
    Task<Destination?> GetDestinationByIdAsync(int id);
    Task<bool> AddDestinationAsync(Destination destination);
    Task<bool> UpdateDestinationAsync(int id,Destination destination);
    Task<bool> DeleteDestinationAsync(int id);
}