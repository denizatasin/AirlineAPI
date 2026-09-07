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
    public async Task<bool> AddPassengerAsync(Passenger passenger)
    {
        _context.Passengers.Add(passenger);
        await _context.SaveChangesAsync();

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
        _context.Passengers.Remove(passenger);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Passenger ID {PassengerId} ({FirstName} {LastName}) deleted.",id,passenger.FirstName,passenger.LastName);
        return true;
    }
}