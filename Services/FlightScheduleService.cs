using AirlineAPI.Data;
using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class FlightScheduleService : IFlightScheduleService
{
    private readonly AirlineDbContext _context;
    private readonly ILogger<FlightScheduleService> _logger;
    public FlightScheduleService(AirlineDbContext context, ILogger<FlightScheduleService> logger)
    {
        _logger=logger;
        _context=context;
    }
    private async Task<(bool Exists,string? errorMessage)> AircraftExists(int aircraftId)
    {
        bool exists=await _context.Aircrafts.AnyAsync(a=>a.Id==aircraftId);
        if(exists)
        {
            return (true,null);
        }
        var availableAircrafts = await GetAvailableAircraftsAsync();
        var table = string.Join("\n", availableAircrafts.Select(a => $"Id: {a.Id} | Model: {a.Model} | Capacity: {a.Capacity}"));

        var errorMessage = $"Aircraft with Id {aircraftId} does not exist.\n\nAvailable aircrafts:\n{table}";
        return (false, errorMessage);
    }
    private static bool IsValidPrice(decimal price)
    {
        return price>=1000 && price<=50000;
    }
    private async Task<List<Aircraft>>GetAvailableAircraftsAsync()
    {
        var assignedAircraftIds=await _context.FlightSchedules.Select(fs=>fs.AircraftId).Distinct().ToListAsync();
        return await _context.Aircrafts.Where(a=>!assignedAircraftIds.Contains(a.Id)).ToListAsync();
    }
    private static bool IsValidDuration(TimeSpan duration)
    {
        return duration >=TimeSpan.FromMinutes(30);
    }
    private record FlightNumberValidationResult(bool IsValid, Destination? MatchedDestination, string? ErrorMessage);
    private async Task<FlightNumberValidationResult> IsValidFlightNumberForRoute(string flightNumber, string departure, string arrival)
    {
        var numericPart = new string(flightNumber.Where(char.IsDigit).ToArray());
        if (!int.TryParse(numericPart, out int number))
        {            
            return new FlightNumberValidationResult(false,null,"Flight number must contain a numeric part.");
        }

        var allDestinations = await _context.Destinations.ToListAsync();
        var matchedDestination = allDestinations.FirstOrDefault(d => number >= d.RangeStart && number <= d.RangeEnd);

        if (matchedDestination == null)
        {
            var allRanges = string.Join("\n", allDestinations.Select(d => $"{d.RangeStart}-{d.RangeEnd} : {d.City}"));
            return new FlightNumberValidationResult(false,null,$"Flight number '{flightNumber}' does not fall within any known route range.\nValid ranges:\n{allRanges}"); 
        }

        bool isValidCombination =
            (departure == "Istanbul" && arrival == matchedDestination.City) ||
            (arrival == "Istanbul" && departure == matchedDestination.City);

        if (!isValidCombination)
        {
            return new FlightNumberValidationResult(false,null,$"Flight number '{flightNumber}' is reserved for Istanbul-{matchedDestination.City} route (range {matchedDestination.RangeStart}-{matchedDestination.RangeEnd}), but you entered {departure}-{arrival}.");
        }

        return new FlightNumberValidationResult(true, matchedDestination,null);
    }
    private async Task<(bool IsValid,string? errorMessage)> IsAircraftAssignedToDifferentCity(int aircraftId, string newCity, int? excludeScheduleId)
    {
        var existingSchedules = await _context.FlightSchedules
            .Include(fs => fs.Destination)
            .Where(fs => fs.AircraftId == aircraftId && fs.Id != excludeScheduleId)
            .ToListAsync();

        foreach (var existing in existingSchedules)
        {
            if (existing.Destination != null && existing.Destination.City != newCity)
            {
                var availableAircrafts = await GetAvailableAircraftsAsync();
                var table = string.Join("\n", availableAircrafts.Select(a => $"Id: {a.Id} | Model: {a.Model} | Capacity: {a.Capacity}"));

                var errorMessage = $"Aircraft {aircraftId} is already assigned to the Istanbul-{existing.Destination.City} route and cannot also be assigned to Istanbul-{newCity}.\n\nAvailable aircrafts:\n{table}";
                return (false, errorMessage);
            }
        }
        return (true,null);
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<FlightSchedule>> GetAllFlightSchedulesAsync()
    {
        return await _context.FlightSchedules.Include(fs=>fs.Aircraft).Include(fs=>fs.Destination).ToListAsync();
    }
    public async Task<FlightSchedule?> GetFlightScheduleByIdAsync(int id)
    {
        var schedule = await _context.FlightSchedules.Include(fs=>fs.Aircraft).Include(fs=>fs.Destination).FirstOrDefaultAsync(fs=>fs.Id==id);
        if(schedule==null)
        {
            _logger.LogWarning("FlightSchedule query failed. Schedule ID {ScheduleId} not found.", id);
        }    
        return schedule;
    }
    public async Task<bool> AddFlightScheduleAsync(FlightSchedule schedule)
    {
        bool exists=await _context.FlightSchedules.AnyAsync(s=>s.FlightNumber==schedule.FlightNumber);
        if(exists)
        {
            _logger.LogWarning("FlightSchedule creation failed. FlightNumber {FlightNumber} already exists.", schedule.FlightNumber);
            return false;
        }
        var aircraftExistsCheck=await AircraftExists(schedule.AircraftId);
        if(!aircraftExistsCheck.Exists)
        {
            _logger.LogWarning("FlightSchedule creation failed. Aircraft ID {AircraftId} does not exists.", schedule.AircraftId);
            throw new InvalidOperationException(aircraftExistsCheck.errorMessage);
        }
        var routeCheck=await IsValidFlightNumberForRoute(schedule.FlightNumber,schedule.Departure,schedule.Arrival);
        if(!routeCheck.IsValid)
        {
            _logger.LogWarning("FlightSchedule creation failed. Route validation error: {Reason}", routeCheck.ErrorMessage);
            throw new InvalidOperationException(routeCheck.ErrorMessage);
        }

        schedule.DestinationId=routeCheck.MatchedDestination!.Id;
        
        var aircraftCheck= await IsAircraftAssignedToDifferentCity(schedule.AircraftId,routeCheck.MatchedDestination.City,null);
        if(!aircraftCheck.IsValid)
        {
            _logger.LogWarning("FlightSchedule creation failed. Aircraft route collision: {Reason}",aircraftCheck.errorMessage);
            throw new InvalidOperationException(aircraftCheck.errorMessage);
        }
        if(!IsValidDuration(schedule.Duration))
        {
            _logger.LogWarning("FlightSchedule creation failed. Invalid duration: {Duration}",schedule.Duration);
            throw new InvalidOperationException($"Flight duration is {schedule.Duration}. It must be at least 30 minutes.");
        }
        if(!IsValidPrice(schedule.Price))
        {
            _logger.LogWarning("FlightSchedule creation failed. Invalid price: {Price}",schedule.Price);
            throw new InvalidOperationException($"Price is {schedule.Price}. It must be between 1000 and 50000.");
        }
        
        _context.FlightSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("FlightSchedule created. ID: {FlightScheduleId}, FlightNumber: {FlightNumber}, Route: {Departure}-{Arrival}",schedule.Id,schedule.FlightNumber,schedule.Departure,schedule.Arrival);
        return true;
    }
    public async Task<bool> UpdateFlightScheduleAsync(int id, FlightSchedule updatedFlightSchedule)
    {
        var existingSchedule = await _context.FlightSchedules.FirstOrDefaultAsync(us => us.Id == id);
        if (existingSchedule == null)
        {
            _logger.LogWarning("FlightSchedule update failed. Schedule ID {FlightScheduleId} not found.",id);
            return false;
        }
        var aircraftExistsCheck=await AircraftExists(updatedFlightSchedule.AircraftId);
        if(!aircraftExistsCheck.Exists)
        {
            _logger.LogWarning("FlightSchedule update failed. Aircraft ID {AircraftId} does not exists.",updatedFlightSchedule.AircraftId);
            throw new InvalidOperationException(aircraftExistsCheck.errorMessage);
        }
        var routeCheck=await IsValidFlightNumberForRoute(updatedFlightSchedule.FlightNumber,updatedFlightSchedule.Departure,updatedFlightSchedule.Arrival);
        if (!routeCheck.IsValid)
        {
            _logger.LogWarning("FlightSchedule update failed. Route validation error: {Reason}",routeCheck.ErrorMessage);
            throw new InvalidOperationException(routeCheck.ErrorMessage);
        } 
        var aircraftCheck=await IsAircraftAssignedToDifferentCity(updatedFlightSchedule.AircraftId, routeCheck.MatchedDestination!.City,id);
        if (!aircraftCheck.IsValid)
        {
            _logger.LogWarning("FlightSchedule update failed. Aircraft route collision: {Reason}",aircraftCheck.errorMessage);
            throw new InvalidOperationException(aircraftCheck.errorMessage);
        }
        if(!IsValidDuration(updatedFlightSchedule.Duration))
        {
            _logger.LogWarning("FlightSchedule update failed. Invalid duration: {Duration}",updatedFlightSchedule.Duration);
            throw new InvalidOperationException($"Flight duration {updatedFlightSchedule.Duration} must be at least 30 minutes.");
        }
        if(!IsValidPrice(updatedFlightSchedule.Price))
        {
            _logger.LogWarning("FlightSchedule update failed. Invalid price: {Price}",updatedFlightSchedule.Price);
            throw new InvalidOperationException($"Price {updatedFlightSchedule.Price} must be between 1000 and 50000.");
        }

        existingSchedule.FlightNumber = updatedFlightSchedule.FlightNumber;
        existingSchedule.Departure = updatedFlightSchedule.Departure;
        existingSchedule.Arrival = updatedFlightSchedule.Arrival;
        existingSchedule.DepartureTime = updatedFlightSchedule.DepartureTime;
        existingSchedule.ArrivalTime = updatedFlightSchedule.ArrivalTime;
        existingSchedule.Price = updatedFlightSchedule.Price;
        existingSchedule.Duration = updatedFlightSchedule.Duration;
        existingSchedule.AircraftId = updatedFlightSchedule.AircraftId;
        existingSchedule.DestinationId = routeCheck.MatchedDestination.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("FlightSchedule ID {FlightScheduleId} updated. FlightNumber: {FlightNumber}",id,updatedFlightSchedule.FlightNumber);
        return true;
    }
    public async Task<bool> DeleteFlightScheduleAsync(int id)
    {
        var schedule=await _context.FlightSchedules.FindAsync(id);
        if(schedule==null)
        {
            _logger.LogWarning("FlightSchedule deletion failed. FlightSchedule ID {FlightScheduleId} not found.",id);
            return false;
        }
        _context.FlightSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("FlightSchedule ID {FlightScheduleId} (FlightNumber: {FlightNumber}) deleted.",id,schedule.FlightNumber);
        return true;
    }
}