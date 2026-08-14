using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface IPassengerService
{
    Task<List<Passenger>> GetAllPassengersAsync();
    Task<Passenger?> GetPassengerByIdAsync(int id);
    Task<bool> AddPassengerAsync(Passenger passenger);
    Task<bool> UpdatePassengerAsync(int id, Passenger updatedPassenger);
    Task<bool> DeletePassengerAsync(int id);
}