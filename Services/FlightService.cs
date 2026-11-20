using AirlineAPI.DTOs;
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
    private readonly ILogger<FlightService> _logger;
    public FlightService(AirlineDbContext context, ILogger<FlightService> logger)
    {
        _context=context;
        _logger=logger;
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
        return await _context.Flights.Include(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).ToListAsync();
    }
    public async Task<Flight?> GetFlightByIdAsync(int id)
    {
        var flight= await _context.Flights.Include(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).FirstOrDefaultAsync(f=>f.Id==id);
        if(flight==null)
        {
            _logger.LogWarning("Flight query failed. Flight ID {FlightId} not found.", id);
        }
        return flight;
    }
    public async Task<bool> AddFlightAsync(Flight flight)
    {
        bool exists=await _context.Flights.AnyAsync(f=>f.FlightScheduleId==flight.FlightScheduleId && f.Date==flight.Date);
        if(exists)
        {
            _logger.LogWarning("Flight creation failed. Flight already exists for Schedule ID {FlightScheduleId} on Date {Date}", flight.FlightScheduleId, flight.Date);
            return false;
        }
        var scheduleValidation=await ValidateFlightScheduleExistsAsync(flight.FlightScheduleId);
        if(!scheduleValidation.isValid)
        {
            _logger.LogWarning("Flight creation failed. Schedule validation error {Reason}",scheduleValidation.errorMessage);
            throw new InvalidOperationException(scheduleValidation.errorMessage);
        }
        if(!IsValidFlightDate(flight.Date))
        {
            _logger.LogWarning("Flight creation failed. Cannot create a flight with past date: {Date}",flight.Date);
            throw new InvalidOperationException($"Cannot create a flight with a past date ({flight.Date}). Date must be today or later.");
        }
        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Flight created successfully. ID: {FlightId}, ScheduleID: {ScheduleId}, Date: {Date}", flight.Id, flight.FlightScheduleId, flight.Date);
        return true;
    }
    public async Task<bool> UpdateFlightAsync(int id,DateOnly newDate)
    {
        var existingFlight=await _context.Flights.FirstOrDefaultAsync(f=>f.Id==id);
        if(existingFlight==null)
        {
            _logger.LogWarning("Flight update failed. Flight ID {FlightId} not found.",id);
            return false;
        }
        var scheduleValidation=await ValidateFlightScheduleExistsAsync(existingFlight.FlightScheduleId);
        if(!scheduleValidation.isValid)
        {
            _logger.LogWarning("Flight update failed. Schedule validation error: {Reason}",scheduleValidation.errorMessage);
            throw new InvalidOperationException(scheduleValidation.errorMessage);
        }
        if(!IsValidFlightDate(newDate))
        {
            _logger.LogWarning("Flight update failed. Invalid past date: {Date}",newDate);
            throw new InvalidOperationException($"Cannot update a flight with a past date ({newDate}). Date must be today or later.");
        }
        var oldDate=existingFlight.Date;
        existingFlight.Date=newDate;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Flight ID {FlightId} date changed from {OldDate} to {NewDate}", id, oldDate, newDate);
        return true;
    }
    // Free seats per flight: aircraft capacity minus active (not cancelled) tickets. Flights need their Aircraft loaded.
    public async Task<Dictionary<int,int>> GetAvailableSeatsAsync(IEnumerable<Flight> flights)
    {
        var activeCounts=await _context.Tickets.Where(t=>t.Status!=TicketStatus.Cancelled)
            .GroupBy(t=>t.FlightId).Select(g=>new{FlightId=g.Key,Count=g.Count()})
            .ToDictionaryAsync(x=>x.FlightId,x=>x.Count);
        return flights.ToDictionary(f=>f.Id,f=>Math.Max(0,f.FlightSchedule.Aircraft.Capacity-(activeCounts.TryGetValue(f.Id,out var used) ? used : 0)));
    }
    public async Task<SeatMapResponse?> GetSeatMapAsync(int flightId)
    {
        var flight=await _context.Flights.Include(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).FirstOrDefaultAsync(f=>f.Id==flightId);
        if(flight==null)
        {
            return null;
        }
        var aircraft=flight.FlightSchedule.Aircraft;
        var activeTickets=await _context.Tickets.Where(t=>t.FlightId==flightId && t.Status!=TicketStatus.Cancelled).Select(t=>t.SeatNumber).ToListAsync();
        var response=new SeatMapResponse
        {
            FlightId=flight.Id,
            FlightNumber=flight.FlightSchedule.FlightNumber,
            Model=aircraft.Model,
            Capacity=aircraft.Capacity,
            BasePrice=flight.FlightSchedule.Price,
            AvailableSeats=Math.Max(0,aircraft.Capacity-activeTickets.Count),
            TakenSeats=activeTickets.Where(s=>s!=null).Select(s=>s!).ToList()
        };
        foreach(var cabin in SeatLayoutBuilder.Build(aircraft.Model,aircraft.Capacity))
        {
            response.Cabins.Add(new SeatCabinResponse
            {
                Name=cabin.Name,
                Layout=cabin.Layout,
                Price=SeatLayoutBuilder.PriceFor(flight.FlightSchedule.Price,cabin.Name),
                Rows=SeatLayoutBuilder.RowsOf(cabin).Select(r=>new SeatRowResponse{Number=r.Number,Seats=r.Seats}).ToList()
            });
        }
        return response;
    }
    public async Task<bool> DeleteFlightAsync(int id)
    {
        var flight=await _context.Flights.FindAsync(id);
        if(flight==null)
        {
            _logger.LogWarning("Flight deletion failed. Flight ID {FlightId} not found.", id);
            return false;
        }
        int ticketCount=await _context.Tickets.CountAsync(t=>t.FlightId==id);
        if(ticketCount>0)
        {
            _logger.LogWarning("Flight deletion failed. Flight ID {FlightId} has {TicketCount} ticket(s).",id,ticketCount);
            throw new InvalidOperationException($"Flight with ID {id} cannot be deleted because {ticketCount} ticket(s) exist for it.");
        }
        _context.Flights.Remove(flight);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Flight ID {FlightId} deleted", id);
        return true;
    }
        public async Task<int> GenerateFlightsAsync(DateOnly startDate,int days)
    {
        var schedules=await _context.FlightSchedules.AsNoTracking().ToListAsync();
        var endDate=startDate.AddDays(days-1);
        var existingFlights=await _context.Flights
            .Where(f=>f.Date>=startDate && f.Date<=endDate)
            .Select(f=>new{f.FlightScheduleId,f.Date})
            .ToListAsync();
        var existingKeys=existingFlights.Select(f=>(f.FlightScheduleId,f.Date)).ToHashSet();

        var now=DateTime.Now;
        var newFlights=new List<Flight>();
        for(int i=0;i<days;i++)
        {
            var date=startDate.AddDays(i);
            foreach(var schedule in schedules)
            {
                if(existingKeys.Contains((schedule.Id,date)))
                {
                    continue;
                }
                var departure=date.ToDateTime(TimeOnly.FromTimeSpan(schedule.DepartureTime));
                if(departure<now)
                {
                    continue;
                }
                newFlights.Add(new Flight{FlightScheduleId=schedule.Id,Date=date});
            }
        }
        if(newFlights.Count>0)
        {
            _context.Flights.AddRange(newFlights);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Flights generated. Created: {Created}, Start date: {StartDate}, Days: {Days}",newFlights.Count,startDate,days);
        return newFlights.Count;
    }
}