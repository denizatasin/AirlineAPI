using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface IAircraftService
{
    Task<List<Aircraft>> GetAllAircraftsAsync();
    Task<Aircraft?> GetAircraftByIdAsync(int id);
    Task<bool> AddAircraftAsync(Aircraft aircraft);
    Task<bool> UpdateAircraftAsync(int id, Aircraft updatedAircraft);
    Task<bool> DeleteAircraftAsync(int id);
}