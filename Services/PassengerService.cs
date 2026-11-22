using AirlineAPI.Data;
using AirlineAPI.DTOs;
using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class PassengerService: IPassengerService
{
    private readonly AirlineDbContext _context;
    private readonly ILogger<PassengerService> _logger;
    public PassengerService(AirlineDbContext context,ILogger<PassengerService> logger)
    {
        _logger=logger;
        _context=context;
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<Passenger>> GetAllPassengersAsync()
    {
        return await _context.Passengers.ToListAsync();
    }
    public async Task<Passenger?> GetPassengerByIdAsync(int id)
    {
        var passenger = await _context.Passengers.FindAsync(id);
        if(passenger==null)
        {
            _logger.LogWarning("Passenger query failed. Passenger ID {PassengerId} not found.",id);
        }
        return passenger;
    }
    // Validates the profile fields and makes sure Email / IdentityNumber are not used by another passenger
    private async Task CheckProfileAsync(Passenger passenger,int? ignoreId)
    {
        var error=PassengerRules.Validate(passenger);
        if(error!=null)
        {
            throw new InvalidOperationException(error);
        }
        if(passenger.Email!=null && await _context.Passengers.AnyAsync(p=>p.Email==passenger.Email && p.Id!=ignoreId))
        {
            throw new InvalidOperationException("Bu e-posta adresi başka bir yolcuya ait.");
        }
        if(passenger.IdentityNumber!=null && await _context.Passengers.AnyAsync(p=>p.IdentityNumber==passenger.IdentityNumber && p.Id!=ignoreId))
        {
            throw new InvalidOperationException("Bu TC kimlik / pasaport numarası başka bir yolcuya ait.");
        }
    }
    public async Task<bool> AddPassengerAsync(Passenger passenger)
    {
        await CheckProfileAsync(passenger,null);
        _context.Passengers.Add(passenger);
        await SaveProfileAsync();

        _logger.LogInformation("Passenger registered successfully. ID: {PassengerId}, Name: {FirstName} {LastName}",passenger.Id,passenger.FirstName,passenger.LastName);
        return true;
    }
    public async Task<bool> UpdatePassengerAsync(int id, Passenger updatedPassenger)
    {
        var existingPassenger=await _context.Passengers.FirstOrDefaultAsync(p=>p.Id==id);
        if(existingPassenger==null)
        {
            _logger.LogWarning("Passenger update failed. Passenger ID {PassengerId} not found.",id);
            return false;
        }
        existingPassenger.FirstName=updatedPassenger.FirstName;
        existingPassenger.LastName=updatedPassenger.LastName;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Passenger ID {PassengerId} profile updated. Name: {FirstName} {LastName}",id,existingPassenger.FirstName,existingPassenger.LastName);
        return true;
    }
    public async Task<bool> DeletePassengerAsync(int id)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger== null)
        {
            _logger.LogWarning("Passenger deletion failed. Passenger ID {PassengerId} not found.",id);
            return false;
        }
        // Tickets are history (and may hold refundable money), so a passenger with tickets cannot be deleted
        int ticketCount=await _context.Tickets.CountAsync(t=>t.PassengerId==id);
        if(ticketCount>0)
        {
            _logger.LogWarning("Passenger deletion failed. Passenger ID {PassengerId} has {TicketCount} ticket(s).",id,ticketCount);
            throw new InvalidOperationException($"Bu yolcunun {ticketCount} bileti var, silinemez. Önce biletleri sil veya iptal et.");
        }
        if(passenger.Balance>0)
        {
            throw new InvalidOperationException($"Bu yolcunun bakiyesinde {passenger.Balance:0.##} TL var. Silmeden önce bakiyeyi 0'a çek.");
        }
        // The login account goes with the passenger (otherwise an orphan account would stay behind)
        var account=await _context.Users.FirstOrDefaultAsync(u=>u.PassengerId==id);
        if(account!=null)
        {
            _context.Users.Remove(account);
        }
        var milesHistory=await _context.MilesTransactions.Where(m=>m.PassengerId==id).ToListAsync();
        _context.MilesTransactions.RemoveRange(milesHistory);
        _context.Passengers.Remove(passenger);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Passenger ID {PassengerId} ({FirstName} {LastName}) deleted.",id,passenger.FirstName,passenger.LastName);
        return true;
    }
    // Adds money to the balance. Returns null if the passenger does not exist.
    public async Task<Passenger?> TopUpBalanceAsync(int id, decimal amount)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger==null)
        {
            return null;
        }
        passenger.Balance+=Math.Round(amount,2);
        await SaveBalanceAsync();
        _logger.LogInformation("Passenger ID {PassengerId} topped up {Amount} TL. New balance: {Balance} TL",id,amount,passenger.Balance);
        return passenger;
    }
    public async Task<bool> SetBalanceAsync(int id, decimal balance)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger==null)
        {
            return false;
        }
        passenger.Balance=Math.Round(balance,2);
        await SaveBalanceAsync();
        _logger.LogInformation("Passenger ID {PassengerId} balance set to {Balance} TL by admin.",id,passenger.Balance);
        return true;
    }
    private async Task SaveBalanceAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Bakiye aynı anda başka bir işlemle değişti. Lütfen tekrar dene.");
        }
    }
    // The database unique indexes are the last safety net if two requests use the same value at the same moment
    private async Task SaveProfileAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateException)
        {
            throw new InvalidOperationException("E-posta veya TC kimlik / pasaport numarası başka bir yolcuya ait.");
        }
    }
    public async Task<PassengerStats?> GetStatsAsync(int id)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger==null)
        {
            return null;
        }
        var tickets=await _context.Tickets.Where(t=>t.PassengerId==id && t.Status!=TicketStatus.Cancelled)
            .Include(t=>t.Flight).ThenInclude(f=>f.FlightSchedule).ToListAsync();
        var now=DateTime.Now;
        int statusMiles=await _context.MilesTransactions.Where(m=>m.PassengerId==id && m.Type=="Earned" && m.CreatedAt>=MembershipTiers.WindowStart()).SumAsync(m=>m.Amount);
        // A ticket counts as flown when its status is Used or its departure time is already in the past
        bool IsFlown(Ticket t)=>t.Status==TicketStatus.Used || t.Flight.Date.ToDateTime(TimeOnly.FromTimeSpan(t.Flight.FlightSchedule.DepartureTime))<now;
        return new PassengerStats
        {
            Passenger=passenger,
            TotalFlights=tickets.Count(IsFlown),
            ActiveTickets=tickets.Count(t=>!IsFlown(t)),
            TotalSpent=tickets.Sum(t=>t.PricePaid),
            StatusMiles=statusMiles
        };
    }
    // Passenger edits their own profile. Locked fields (nationality, identity number, birth date) can only be filled while empty.
    public async Task<Passenger?> UpdateMyProfileAsync(int id, UpdateMyProfileRequest request)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger==null)
        {
            return null;
        }
        passenger.Email=PassengerRules.NormalizeEmail(request.Email);
        passenger.Phone=PassengerRules.NormalizePhone(request.Phone);
        passenger.Gender=PassengerRules.NormalizeText(request.Gender);

        var nationality=PassengerRules.NormalizeText(request.Nationality);
        var identity=PassengerRules.NormalizeIdentity(request.IdentityNumber);
        if(nationality!=null && nationality!=passenger.Nationality)
        {
            if(passenger.Nationality!=null) throw new InvalidOperationException("Uyruğu sadece admin değiştirebilir.");
            passenger.Nationality=nationality;
        }
        if(identity!=null && identity!=passenger.IdentityNumber)
        {
            if(passenger.IdentityNumber!=null) throw new InvalidOperationException("TC kimlik / pasaport numarasını sadece admin değiştirebilir.");
            passenger.IdentityNumber=identity;
        }
        if(request.BirthDate!=null && request.BirthDate!=passenger.BirthDate)
        {
            if(passenger.BirthDate!=null) throw new InvalidOperationException("Doğum tarihini sadece admin değiştirebilir.");
            passenger.BirthDate=request.BirthDate;
        }
        await CheckProfileAsync(passenger,id);
        await SaveProfileAsync();
        _logger.LogInformation("Passenger ID {PassengerId} updated their profile.",id);
        return passenger;
    }
    // The latest 50 lines of the passenger's miles history, newest first
    public async Task<List<MilesTransaction>> GetMilesHistoryAsync(int id)
    {
        return await _context.MilesTransactions.Where(m=>m.PassengerId==id).OrderByDescending(m=>m.CreatedAt).ThenByDescending(m=>m.Id).Take(50).ToListAsync();
    }
    // PassengerId -> login name, for the admin passenger list
    public async Task<Dictionary<int,string>> GetUsernamesAsync()
    {
        return await _context.Users.Where(u=>u.PassengerId!=null).ToDictionaryAsync(u=>u.PassengerId!.Value,u=>u.Username);
    }
}
