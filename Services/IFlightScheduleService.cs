using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface IFlightScheduleService
{
    Task<List<FlightSchedule>> GetAllFlightSchedulesAsync();
    Task<FlightSchedule?> GetFlightScheduleByIdAsync(int id);
    Task<bool> AddFlightScheduleAsync(FlightSchedule flightSchedule);
    Task<bool> UpdateFlightScheduleAsync(int id, FlightSchedule updatedFlightSchedule);
    Task<bool> DeleteFlightScheduleAsync(int id);
}