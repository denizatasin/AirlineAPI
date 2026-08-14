using AirlineAPI.Models;

namespace AirlineAPI.Services;

/// <summary>
/// Defines the contract for flight operations.
/// Any class implementing this interface must provide these methods.
/// </summary>
public interface IFlightService
{
    Task<List<Flight>> GetAllFlightsAsync();
    Task<Flight?> GetFlightByIdAsync(int id);
    Task<bool> AddFlightAsync(Flight flight);
    Task<bool> UpdateFlightAsync(int id,DateOnly newDate);
    Task<bool> DeleteFlightAsync(int id);
}