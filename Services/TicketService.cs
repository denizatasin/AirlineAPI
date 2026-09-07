using AirlineAPI.Models;
using AirlineAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class TicketService : ITicketService
{
    private readonly AirlineDbContext _context;
    private readonly ILogger<TicketService> _logger;
    public TicketService (AirlineDbContext context, ILogger<TicketService> logger)
    {
        _context=context;
        _logger=logger;
    }
    private async Task<(bool isValid,string? errorMessage)> ValidateFlightForTicketAsync(int flightId)
    {
        var flight=await _context.Flights.Include(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).FirstOrDefaultAsync(f=>f.Id==flightId);
        if(flight==null)
        {
            var availableFlights=await _context.Flights.Include(f=>f.FlightSchedule).Where(f=>f.Date>=DateOnly.FromDateTime(DateTime.Now)).OrderBy(f=>f.Date).ThenBy(f=>f.Id).ToListAsync();
            availableFlights=availableFlights.Where(f=>GetFlightDateTime(f)>=DateTime.Now).ToList();
            if(!availableFlights.Any())
            {
                return (false, $"Flight with ID '{flightId}' not found. There are currently no flights available in the system.");
            }
            var table = string.Join("\n", availableFlights.Select(f =>$"Id: {f.Id} | Date: {f.Date} | Flight: {f.FlightSchedule.FlightNumber} ({f.FlightSchedule.Departure} -> {f.FlightSchedule.Arrival}) | Departure Time: {f.FlightSchedule.DepartureTime:hh\\:mm}"));
            var errorMessage = $"Flight with Id '{flightId}' not found.\n\nAvailable Flights:\n{table}";
            return (false, errorMessage);        
        }
        if (GetFlightDateTime(flight) < DateTime.Now)
        {
            return (false, $"Cannot purchase a ticket for flight {flightId} ({flight.FlightSchedule.FlightNumber} on {flight.Date}) because it has already departed.");
        }
        int capacity=flight.FlightSchedule.Aircraft.Capacity;
        int soldTickets= await _context.Tickets.CountAsync(t=>t.FlightId==flightId && t.Status!=TicketStatus.Cancelled);
        if(soldTickets>=capacity)
        {
            return (false, $"Flight {flightId} ({flight.FlightSchedule.FlightNumber} on {flight.Date}) has no available seats. (Capacity: {capacity}/{capacity})");
        }
        return (true,null);
    }
    private async Task<(bool isValid,string? errorMessage)>ValidatePassengerExistsAsync(int passengerId)
    {
        bool passengerExists=await _context.Passengers.AnyAsync(p=>p.Id==passengerId);
        if(passengerExists)
        {
            return(true,null);
        }
        var availablePassengers=await _context.Passengers.OrderBy(p=>p.Id).ToListAsync();
        if(!availablePassengers.Any())
        {
            return (false, $"Passenger with ID '{passengerId}' not found. There are currently no registered passengers in the system.");
        }
        var table = string.Join("\n", availablePassengers.Select(p =>$"Id: {p.Id} | Name: {p.FirstName} {p.LastName} "));
        var errorMessage = $"Passenger with Id '{passengerId}' not found.\n\nAvailable Passengers:\n{table}";
        return (false, errorMessage);
    }
    private async Task<(bool isValid,string? errorMessage)>ValidatePricePaidAsync(int flightId,decimal pricePaid)
    {
        var flight=await _context.Flights.Include(f=>f.FlightSchedule).FirstOrDefaultAsync(f=>f.Id==flightId);
        if(flight==null)
        {
            return (true,null);
        }
        decimal expectedPrice=flight.FlightSchedule.Price;
        if(pricePaid!=expectedPrice)
        {
            return (false, $"Invalid payment amount. The official price for flight {flightId} is {expectedPrice:N2} TL, but you entered {pricePaid:N2} TL.");
        }
        return(true,null);
    }
    private async Task UpdateExpiredTicketStatusAsync()
    {
        var now=DateTime.Now;
        var candidateTickets=await _context.Tickets.Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).Where(t=>t.Status==TicketStatus.Booked || t.Status==TicketStatus.CheckedIn).ToListAsync();
        bool changed=false;
        foreach(var ticket in candidateTickets)
        {
            var flightDate=ticket.Flight.Date;
            var departureTime=ticket.Flight.FlightSchedule.DepartureTime;
            var flightDateTime=GetFlightDateTime(ticket.Flight);
            if(flightDateTime<now)
            {
                ticket.Status=TicketStatus.Used;
                changed=true;
            }
        }
        if(changed)
        {
            await _context.SaveChangesAsync();
        }
    }
    private static DateTime GetFlightDateTime(Flight flight)
    {
        return flight.Date.ToDateTime(TimeOnly.FromTimeSpan(flight.FlightSchedule.DepartureTime));
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<Ticket>> GetAllTicketsAsync()
    {
        await UpdateExpiredTicketStatusAsync();
        return await _context.Tickets.Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).Include(t=>t.Passenger).ToListAsync();
    }
    public async Task<Ticket?> GetTicketByIdAsync(int id)
    {
        await UpdateExpiredTicketStatusAsync();
        return await _context.Tickets
            .Include(t=>t.Flight)
                .ThenInclude(f=>f.FlightSchedule)
                .ThenInclude(fs=>fs.Aircraft)
            .Include(t=>t.Passenger).FirstOrDefaultAsync(t=>t.Id==id);
    }
    public async Task<bool> AddTicketAsync(Ticket ticket)
    {
        var flightValidation= await ValidateFlightForTicketAsync(ticket.FlightId);
        if(!flightValidation.isValid)
        {
            _logger.LogWarning("Ticket purchase failed due to flight validation {Reason}", flightValidation.errorMessage);
            throw new InvalidOperationException(flightValidation.errorMessage);
        }
        var passengerValidation=await ValidatePassengerExistsAsync(ticket.PassengerId);
        if(!passengerValidation.isValid)
        {
            _logger.LogWarning("Ticket purchase failed due to passenger validation {Reason}", passengerValidation.errorMessage);
            throw new InvalidOperationException(passengerValidation.errorMessage);
        }
        var priceValidation=await ValidatePricePaidAsync(ticket.FlightId,ticket.PricePaid);
        if(!priceValidation.isValid)
        {
            _logger.LogWarning("Ticket purchase failed due to price validation {Reason}", priceValidation.errorMessage);
            throw new InvalidOperationException(priceValidation.errorMessage);
        }
        var existingNumbers = await _context.Tickets
            .Select(t => t.TicketNumber)
            .ToListAsync();

        int nextSerial = existingNumbers.Any()
            ? existingNumbers.Max(tn => int.Parse(tn.Substring(tn.Length - 4))) + 1
            : 1;

        ticket.TicketNumber = $"235-{DateTime.Now:HHmmss}{nextSerial:D4}";

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket created succesfully: {TicketNumber} for Passenger {PassengerId} on Flight {FlightId}"
            ,ticket.TicketNumber, ticket.PassengerId, ticket.FlightId);
        
        return true;
    }
    public async Task<bool> UpdateTicketAsync(int id,TicketStatus newStatus)
    {
        var existingTicket=await _context.Tickets.FirstOrDefaultAsync(t=>t.Id==id);
        if(existingTicket==null)
        {
            _logger.LogWarning("Ticket update failed. Ticket ID {TicketId} not found.", id);
            return false;
        }
        var oldStatus=existingTicket.Status;
        existingTicket.Status=newStatus;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket ID {TicketId} status changed from {OldStatus} to {NewStatus}", id, oldStatus, newStatus);
        return true;
    }
    public async Task<bool> DeleteTicketAsync(int id)
    {
        var ticket=await _context.Tickets.FindAsync(id);
        if(ticket==null)
        {
            _logger.LogWarning("Ticket deletion failed. Ticket ID {TicketId} not found.", id);
            return false;
        }
        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket ID {TicketId} (Number:{TicketNumber}) deleted by admin.", id, ticket.TicketNumber);
        return true;
    }
}