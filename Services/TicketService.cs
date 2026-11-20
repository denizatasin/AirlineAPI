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
    // The price depends on the cabin of the chosen seat: base price x cabin multiplier
    private async Task<(bool isValid,string? errorMessage)>ValidatePricePaidAsync(Ticket ticket)
    {
        var flight=await _context.Flights.Include(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).FirstOrDefaultAsync(f=>f.Id==ticket.FlightId);
        if(flight==null)
        {
            return (true,null);
        }
        var cabins=SeatLayoutBuilder.Build(flight.FlightSchedule.Aircraft.Model,flight.FlightSchedule.Aircraft.Capacity);
        string cabinName=ticket.SeatNumber!=null ? (SeatLayoutBuilder.CabinOfSeat(cabins,ticket.SeatNumber) ?? "Economy") : "Economy";
        decimal expectedPrice=SeatLayoutBuilder.PriceFor(flight.FlightSchedule.Price,cabinName);
        if(ticket.PricePaid!=expectedPrice)
        {
            return (false, $"Invalid payment amount. The official price for seat {ticket.SeatNumber} ({cabinName}) on flight {ticket.FlightId} is {expectedPrice:N2} TL, but you entered {ticket.PricePaid:N2} TL.");
        }
        return(true,null);
    }
    // Checks the requested seat (or picks a random free one) and writes it into the ticket
    private async Task<(bool isValid,string? errorMessage)> AssignSeatAsync(Ticket ticket)
    {
        var aircraft=await _context.Flights.Where(f=>f.Id==ticket.FlightId).Select(f=>f.FlightSchedule.Aircraft).FirstAsync();
        var cabins=SeatLayoutBuilder.Build(aircraft.Model,aircraft.Capacity);
        var allSeats=SeatLayoutBuilder.AllSeatNumbers(cabins);
        // No seat requested => random seat, but only in Economy (the cheapest cabin)
        var randomPool=string.IsNullOrWhiteSpace(ticket.SeatNumber) ? SeatLayoutBuilder.EconomySeatNumbers(cabins) : null;
        var activeSeats=await _context.Tickets.Where(t=>t.FlightId==ticket.FlightId && t.Status!=TicketStatus.Cancelled).Select(t=>t.SeatNumber).ToListAsync();
        var choice=SeatLayoutBuilder.ChooseSeat(allSeats,activeSeats.Where(s=>s!=null).Select(s=>s!),ticket.SeatNumber,randomPool);
        if(choice.error!=null)
        {
            return (false,choice.error);
        }
        ticket.SeatNumber=choice.seat;
        return (true,null);
    }
    // A cancelled ticket can only come back if the flight still has room and its seat is still free
    private async Task<(bool isValid,string? errorMessage)> ValidateReactivationAsync(Ticket ticket)
    {
        int capacity=await _context.Flights.Where(f=>f.Id==ticket.FlightId).Select(f=>f.FlightSchedule.Aircraft.Capacity).FirstAsync();
        int activeTickets=await _context.Tickets.CountAsync(t=>t.FlightId==ticket.FlightId && t.Status!=TicketStatus.Cancelled);
        if(activeTickets>=capacity)
        {
            return (false,$"Ticket {ticket.TicketNumber} cannot be reactivated because the flight has no available seats. (Capacity: {capacity}/{capacity})");
        }
        if(ticket.SeatNumber!=null)
        {
            bool seatTaken=await _context.Tickets.AnyAsync(t=>t.Id!=ticket.Id && t.FlightId==ticket.FlightId && t.Status!=TicketStatus.Cancelled && t.SeatNumber==ticket.SeatNumber);
            if(seatTaken)
            {
                return (false,$"Ticket {ticket.TicketNumber} cannot be reactivated because seat {ticket.SeatNumber} is now taken by another ticket.");
            }
        }
        return (true,null);
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
    public async Task<bool> AddTicketAsync(Ticket ticket,bool chargeBalance=false)
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
        var seatValidation=await AssignSeatAsync(ticket);
        if(!seatValidation.isValid)
        {
            _logger.LogWarning("Ticket purchase failed due to seat validation {Reason}", seatValidation.errorMessage);
            throw new InvalidOperationException(seatValidation.errorMessage);
        }
        var priceValidation=await ValidatePricePaidAsync(ticket);
        if(!priceValidation.isValid)
        {
            _logger.LogWarning("Ticket purchase failed due to price validation {Reason}", priceValidation.errorMessage);
            throw new InvalidOperationException(priceValidation.errorMessage);
        }
        if(chargeBalance)
        {
            var payer=await _context.Passengers.FindAsync(ticket.PassengerId);
            if(payer==null || payer.Balance<ticket.PricePaid)
            {
                decimal current=payer?.Balance ?? 0;
                _logger.LogWarning("Ticket purchase failed: insufficient balance for Passenger {PassengerId}",ticket.PassengerId);
                throw new InvalidOperationException($"Insufficient balance. The ticket costs {ticket.PricePaid:N2} TL but your balance is {current:N2} TL. Please add balance first.");
            }
            payer.Balance-=ticket.PricePaid;
            ticket.BalanceCharged=true;
        }
        var existingNumbers = await _context.Tickets
            .Select(t => t.TicketNumber)
            .ToListAsync();

        int nextSerial = existingNumbers.Any()
            ? existingNumbers.Max(tn => int.Parse(tn.Substring(tn.Length - 4))) + 1
            : 1;

        ticket.TicketNumber = $"235-{DateTime.Now:HHmmss}{nextSerial:D4}";

        try
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateConcurrencyException)
        {
            // The balance was changed by another request while this ticket was being bought
            throw new InvalidOperationException("Your balance changed while the ticket was being bought. Please try again.");
        }
        catch(DbUpdateException ex)
        {
            // Two people bought the same seat at the same moment: the unique index stopped the second one
            _logger.LogWarning(ex,"Ticket could not be saved for seat {SeatNumber} on Flight {FlightId}.",ticket.SeatNumber,ticket.FlightId);
            throw new InvalidOperationException($"The ticket could not be saved. Seat '{ticket.SeatNumber}' was most likely taken by another passenger a moment ago. Please try again.");
        }

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
        if(oldStatus==TicketStatus.Cancelled && newStatus!=TicketStatus.Cancelled)
        {
            var reactivation=await ValidateReactivationAsync(existingTicket);
            if(!reactivation.isValid)
            {
                _logger.LogWarning("Ticket ID {TicketId} reactivation failed: {Reason}", id, reactivation.errorMessage);
                throw new InvalidOperationException(reactivation.errorMessage);
            }
        }
        // Money side: cancelling refunds a ticket that was paid from the balance, reactivating charges it again
        if(existingTicket.BalanceCharged)
        {
            var owner=await _context.Passengers.FindAsync(existingTicket.PassengerId);
            if(owner!=null)
            {
                if(oldStatus!=TicketStatus.Cancelled && newStatus==TicketStatus.Cancelled)
                {
                    owner.Balance+=existingTicket.PricePaid;
                }
                else if(oldStatus==TicketStatus.Cancelled && newStatus!=TicketStatus.Cancelled)
                {
                    if(owner.Balance<existingTicket.PricePaid)
                    {
                        throw new InvalidOperationException($"Ticket {existingTicket.TicketNumber} cannot be reactivated because the passenger's balance ({owner.Balance:N2} TL) is lower than the ticket price ({existingTicket.PricePaid:N2} TL).");
                    }
                    owner.Balance-=existingTicket.PricePaid;
                }
            }
        }
        existingTicket.Status=newStatus;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The passenger's balance changed at the same time. Please try again.");
        }

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
        // Deleting an active ticket that was paid from the balance gives the money back
        if(ticket.BalanceCharged && ticket.Status!=TicketStatus.Cancelled)
        {
            var owner=await _context.Passengers.FindAsync(ticket.PassengerId);
            if(owner!=null)
            {
                owner.Balance+=ticket.PricePaid;
            }
        }
        _context.Tickets.Remove(ticket);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The passenger's balance changed at the same time. Please try again.");
        }

        _logger.LogInformation("Ticket ID {TicketId} (Number:{TicketNumber}) deleted by admin.", id, ticket.TicketNumber);
        return true;
    }
}