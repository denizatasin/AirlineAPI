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
    private void AddMilesTransaction(int passengerId,int amount,string type,string description)
    {
        _context.MilesTransactions.Add(new MilesTransaction
        {
            PassengerId=passengerId,
            Amount=amount,
            Type=type,
            Description=description,
            CreatedAt=DateTime.Now
        });
    }
    // The cash part of a ticket: price minus the discount that was paid with miles
    private static decimal CashPart(Ticket ticket)
    {
        return ticket.PricePaid-MilesRules.Discount(ticket.MilesUsed);
    }
    // A ticket that is cancelled/deleted gives back the cash and the miles that were spent on it
    private void RefundTicket(Ticket ticket,Passenger owner)
    {
        owner.Balance+=CashPart(ticket);
        if(ticket.MilesUsed>0)
        {
            owner.Miles+=ticket.MilesUsed;
            AddMilesTransaction(owner.Id,ticket.MilesUsed,"Refunded",$"Ticket {ticket.TicketNumber} cancelled: miles returned");
        }
    }
    // Gives the miles of a flown ticket to the passenger - exactly once per ticket (MilesAwarded).
    // The ticket must be loaded with Flight -> FlightSchedule -> Aircraft and Destination.
    private async Task AwardMilesAsync(Ticket ticket)
    {
        if(ticket.MilesAwarded)
        {
            return;
        }
        var owner=await _context.Passengers.FindAsync(ticket.PassengerId);
        if(owner==null)
        {
            return;
        }
        var schedule=ticket.Flight.FlightSchedule;
        var cabins=SeatLayoutBuilder.Build(schedule.Aircraft.Model,schedule.Aircraft.Capacity);
        string cabinName=ticket.SeatNumber!=null ? (SeatLayoutBuilder.CabinOfSeat(cabins,ticket.SeatNumber) ?? "Economy") : "Economy";
        int miles=MilesRules.Earned(schedule.Destination.DistanceKm,cabinName);
        owner.Miles+=miles;
        ticket.MilesAwarded=true;
        AddMilesTransaction(owner.Id,miles,"Earned",$"Flight {schedule.FlightNumber} {schedule.Departure} - {schedule.Arrival} ({cabinName})");
        // Membership bonus: the level is decided by the flight miles of the last 12 months (before this flight)
        int statusMiles=await _context.MilesTransactions.Where(m=>m.PassengerId==owner.Id && m.Type=="Earned" && m.CreatedAt>=MembershipTiers.WindowStart()).SumAsync(m=>m.Amount);
        string tier=MembershipTiers.TierFor(statusMiles);
        int bonus=(int)Math.Round(miles*MembershipTiers.BonusPercent(tier)/100.0,MidpointRounding.AwayFromZero);
        if(bonus>0)
        {
            owner.Miles+=bonus;
            AddMilesTransaction(owner.Id,bonus,"Bonus",$"{tier} membership bonus (+{MembershipTiers.BonusPercent(tier)}%) for flight {schedule.FlightNumber}");
        }
        _logger.LogInformation("Passenger {PassengerId} earned {Miles} miles for ticket {TicketNumber}.",owner.Id,miles,ticket.TicketNumber);
    }
    private async Task UpdateExpiredTicketStatusAsync()
    {
        var now=DateTime.Now;
        var candidateTickets=await _context.Tickets.Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft).Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ThenInclude(fs=>fs.Destination).Where(t=>t.Status==TicketStatus.Booked || t.Status==TicketStatus.CheckedIn).ToListAsync();
        bool changed=false;
        foreach(var ticket in candidateTickets)
        {
            var flightDate=ticket.Flight.Date;
            var departureTime=ticket.Flight.FlightSchedule.DepartureTime;
            var flightDateTime=GetFlightDateTime(ticket.Flight);
            if(flightDateTime<now)
            {
                ticket.Status=TicketStatus.Used;
                await AwardMilesAsync(ticket);
                changed=true;
            }
        }
        if(changed)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                // Someone changed the passenger's miles/balance at the same moment: forget these changes, the next request redoes them
                _context.ChangeTracker.Clear();
            }
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
        if(ticket.MilesUsed>0 && !chargeBalance)
        {
            throw new InvalidOperationException("Miles can only be used by a passenger buying their own ticket.");
        }
        if(chargeBalance)
        {
            var payer=await _context.Passengers.FindAsync(ticket.PassengerId);
            if(payer==null)
            {
                throw new InvalidOperationException("Passenger not found.");
            }
            if(ticket.MilesUsed>0)
            {
                int maxUsable=MilesRules.MaxUsable(ticket.PricePaid,payer.Miles);
                if(ticket.MilesUsed>maxUsable)
                {
                    _logger.LogWarning("Ticket purchase failed: Passenger {PassengerId} tried to use {Miles} miles (max {Max}).",ticket.PassengerId,ticket.MilesUsed,maxUsable);
                    throw new InvalidOperationException($"You can use at most {maxUsable} miles on this ticket. (You have {payer.Miles} miles, and miles can pay at most {MilesRules.MaxDiscountRatio:P0} of the price.)");
                }
            }
            decimal cash=CashPart(ticket);
            if(payer.Balance<cash)
            {
                _logger.LogWarning("Ticket purchase failed: insufficient balance for Passenger {PassengerId}",ticket.PassengerId);
                throw new InvalidOperationException($"Insufficient balance. You need to pay {cash:N2} TL but your balance is {payer.Balance:N2} TL. Please add balance first.");
            }
            payer.Balance-=cash;
            payer.Miles-=ticket.MilesUsed;
            ticket.BalanceCharged=true;
        }
        var existingNumbers = await _context.Tickets
            .Select(t => t.TicketNumber)
            .ToListAsync();

        int nextSerial = existingNumbers.Any()
            ? existingNumbers.Max(tn => int.Parse(tn.Substring(tn.Length - 4))) + 1
            : 1;

        ticket.TicketNumber = $"235-{DateTime.Now:HHmmss}{nextSerial:D4}";
        if(ticket.MilesUsed>0)
        {
            AddMilesTransaction(ticket.PassengerId,-ticket.MilesUsed,"Spent",$"Ticket {ticket.TicketNumber}: {ticket.MilesUsed} miles used for a {MilesRules.Discount(ticket.MilesUsed):N2} TL discount");
        }

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
        var existingTicket=await _context.Tickets
            .Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ThenInclude(fs=>fs.Aircraft)
            .Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ThenInclude(fs=>fs.Destination)
            .FirstOrDefaultAsync(t=>t.Id==id);
        if(existingTicket==null)
        {
            _logger.LogWarning("Ticket update failed. Ticket ID {TicketId} not found.", id);
            return false;
        }
        var oldStatus=existingTicket.Status;
        if(oldStatus==TicketStatus.Used && newStatus==TicketStatus.Cancelled)
        {
            throw new InvalidOperationException($"Ticket {existingTicket.TicketNumber} has already been used (the flight is done), so it cannot be cancelled.");
        }
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
                    RefundTicket(existingTicket,owner);
                }
                else if(oldStatus==TicketStatus.Cancelled && newStatus!=TicketStatus.Cancelled)
                {
                    decimal cash=CashPart(existingTicket);
                    if(owner.Balance<cash)
                    {
                        throw new InvalidOperationException($"Ticket {existingTicket.TicketNumber} cannot be reactivated because the passenger's balance ({owner.Balance:N2} TL) is lower than the amount to pay ({cash:N2} TL).");
                    }
                    if(owner.Miles<existingTicket.MilesUsed)
                    {
                        throw new InvalidOperationException($"Ticket {existingTicket.TicketNumber} cannot be reactivated because the passenger has only {owner.Miles} miles but {existingTicket.MilesUsed} miles were used on it.");
                    }
                    owner.Balance-=cash;
                    if(existingTicket.MilesUsed>0)
                    {
                        owner.Miles-=existingTicket.MilesUsed;
                        AddMilesTransaction(owner.Id,-existingTicket.MilesUsed,"Spent",$"Ticket {existingTicket.TicketNumber} reactivated: miles used again");
                    }
                }
            }
        }
        existingTicket.Status=newStatus;
        // The flight is flown: the passenger earns miles (only once per ticket)
        if(newStatus==TicketStatus.Used)
        {
            await AwardMilesAsync(existingTicket);
        }
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The passenger's balance or miles changed at the same time. Please try again.");
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
        // (a ticket that was already flown or cancelled is not refunded)
        if(ticket.BalanceCharged && (ticket.Status==TicketStatus.Booked || ticket.Status==TicketStatus.CheckedIn))
        {
            var owner=await _context.Passengers.FindAsync(ticket.PassengerId);
            if(owner!=null)
            {
                RefundTicket(ticket,owner);
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