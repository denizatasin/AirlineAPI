using AirlineAPI.Models;
using AirlineAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

/// <summary>
/// Implements flight-related business operations.
/// Communicates with the database through Entity Framework Core
/// using AirlineDbContext.
/// </summary>
public class FlightService : IFlightService
{
    private readonly AirlineDbContext _context;
    public FlightService(AirlineDbContext context)
    {
        _context=context;
    }
    private static bool IsValidFlightDate(DateOnly date)
    {
        return date>=DateOnly.FromDateTime(DateTime.Now);
    }
    private async Task<(bool isValid,string? errorMessage)>ValidateFlightScheduleExistsAsync(int flightScheduleId)
    {
        var scheduleExists=await _context.FlightSchedules.AnyAsync(fs=>fs.Id==flightScheduleId);
        if(scheduleExists)
        {
            return(true,null);
        }
        var availableSchedules=await _context.FlightSchedules.Include(fs=>fs.Aircraft).OrderBy(s => s.Id).ToListAsync();
        if(!availableSchedules.Any())
        {
            return(false,$"Flight schedule with Id {flightScheduleId} not found. There are currently no available flight schedules in the system.");
        }
        var table = string.Join("\n", availableSchedules.Select(s => $"Id: {s.Id} | Route: {s.Departure} -> {s.Arrival} | Time: {s.DepartureTime:hh\\:mm} - {s.ArrivalTime:hh\\:mm} | Aircraft: {s.Aircraft?.Model ?? "N/A"} (AircraftId: {s.AircraftId})"));
        var errorMessage = $"FlightSchedule with Id '{flightScheduleId}' not found.\n\nAvailable Flight Schedules:\n{table}";
        return (false,errorMessage);    
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<Flight>> GetAllFlightsAsync()
    {
        return await _context.Flights.Include(f=>f.FlightSchedule).ToListAsync();
    }
    public async Task<Flight?> GetFlightByIdAsync(int id)
    {
        return await _context.Flights.Include(f=>f.FlightSchedule).FirstOrDefaultAsync(f=>f.Id==id);
    }
    public async Task<bool> AddFlightAsync(Flight flight)
    {
        bool exists=await _context.Flights.AnyAsync(f=>f.FlightScheduleId==flight.FlightScheduleId && f.Date==flight.Date);
        if(exists)
        {
            return false;
        }
        var scheduleValidation=await ValidateFlightScheduleExistsAsync(flight.FlightScheduleId);
        if(!scheduleValidation.isValid)
        {
            throw new InvalidOperationException(scheduleValidation.errorMessage);
        }
        if(!IsValidFlightDate(flight.Date))
        {
            throw new InvalidOperationException($"Cannot create a flight with a past date ({flight.Date}). Date must be today or later.");
        }
        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateFlightAsync(int id,DateOnly newDate)
    {
        var existingFlight=await _context.Flights.FirstOrDefaultAsync(f=>f.Id==id);
        if(existingFlight==null)
        {
            return false;
        }
        var scheduleValidation=await ValidateFlightScheduleExistsAsync(existingFlight.FlightScheduleId);
        if(!scheduleValidation.isValid)
        {
            throw new InvalidOperationException(scheduleValidation.errorMessage);
        }
        if(!IsValidFlightDate(newDate))
        {
            throw new InvalidOperationException($"Cannot update a flight with a past date ({newDate}). Date must be today or later.");
        }
        existingFlight.Date=newDate;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteFlightAsync(int id)
    {
        var flight=await _context.Flights.FindAsync(id);
        if(flight==null)
        {
            return false;
        }
        _context.Flights.Remove(flight);
        await _context.SaveChangesAsync();
        return true;
    }
}