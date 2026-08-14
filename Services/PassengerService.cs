using AirlineAPI.Data;
using AirlineAPI.DTOs;
using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class PassengerService: IPassengerService
{
    private readonly AirlineDbContext _context;
    
    public PassengerService(AirlineDbContext context)
    {
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
        return await _context.Passengers.FindAsync(id);
    }
    public async Task<bool> AddPassengerAsync(Passenger passenger)
    {
        _context.Passengers.Add(passenger);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdatePassengerAsync(int id, Passenger updatedPassenger)
    {
        var existingPassenger=await _context.Passengers.FirstOrDefaultAsync(p=>p.Id==id);
        if(existingPassenger==null)
        {
            return false;
        }
        existingPassenger.FirstName=updatedPassenger.FirstName;
        existingPassenger.LastName=updatedPassenger.LastName;

        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeletePassengerAsync(int id)
    {
        var passenger=await _context.Passengers.FindAsync(id);
        if(passenger== null)
        {
            return false;
        }
        _context.Passengers.Remove(passenger);
        await _context.SaveChangesAsync();
        return true;
    }
}