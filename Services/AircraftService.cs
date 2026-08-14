using AirlineAPI.Data;
using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class AircraftService : IAircraftService
{
    private readonly AirlineDbContext _context;
    public AircraftService(AirlineDbContext context)
    {
        _context=context;
    }
    private static readonly Dictionary<string,string[]> ValidModelsByManufacturer=new()
    {
        {"Boeing",new[]{"B737-800","B737 MAX 8","B737 MAX 9","B737-900ER","B777-300ER","B787-9 Dreamliner"}},
        {"Airbus",new[]{"A320-200","A320neo","A321-200","A321neo","A330-200","A330-300","A350-900"}}
    };
    private static bool IsValidModel(string manufacturer,string model)
    {
        return ValidModelsByManufacturer.TryGetValue(manufacturer,out var models) && models.Contains(model);
    }
    private static readonly string[] ValidManufacturers={"Boeing","Airbus"};
    private static bool IsValidManufacturer(string manufacturer)
    {
        return ValidManufacturers.Contains(manufacturer);
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<Aircraft>> GetAllAircraftsAsync()
    {
        return await _context.Aircrafts.ToListAsync();
    }
    public async Task<Aircraft?> GetAircraftByIdAsync(int id)
    {
        return await _context.Aircrafts.FindAsync(id);
    }
    public async Task<bool> AddAircraftAsync(Aircraft aircraft)
    {
        if(!IsValidManufacturer(aircraft.Manufacturer))
        {
            throw new InvalidOperationException($"Invalid manufacturer '{aircraft.Manufacturer}'. Only 'Airbus' and 'Boeing' are accepted."); 
        }
        if(!IsValidModel(aircraft.Manufacturer,aircraft.Model))
        {
            throw new InvalidOperationException($"Invalid model '{aircraft.Model}' for manufacturer '{aircraft.Manufacturer}'.");
        }
        bool exists=await _context.Aircrafts.AnyAsync(a=>a.TailNumber==aircraft.TailNumber);
        if(exists)
        {
            return false;
        }
        _context.Aircrafts.Add(aircraft);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateAircraftAsync(int id, Aircraft updatedAircraft)
    {
        var existingAircraft=await _context.Aircrafts.FirstOrDefaultAsync(a=>a.Id==id);
        if(existingAircraft==null)
        {
            return false;
        }
        if(!IsValidManufacturer(updatedAircraft.Manufacturer))
        {
            throw new InvalidOperationException($"Invalid manufacturer '{updatedAircraft.Manufacturer}'. Only 'Airbus' and 'Boeing' are accepted."); 
        } 
        if(!IsValidModel(updatedAircraft.Manufacturer,updatedAircraft.Model))
        {
            throw new InvalidOperationException($"Invalid model '{updatedAircraft.Model}' for manufacturer '{updatedAircraft.Manufacturer}'.");
        } 
        existingAircraft.Manufacturer=updatedAircraft.Manufacturer;
        existingAircraft.Model=updatedAircraft.Model;
        existingAircraft.Capacity=updatedAircraft.Capacity;
        existingAircraft.TailNumber=updatedAircraft.TailNumber;

        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAircraftAsync(int id)
    {
        var aircraft=await _context.Aircrafts.FindAsync(id);
        if(aircraft==null)
        {
            return false;
        }
        bool isInUse=await _context.FlightSchedules.AnyAsync(fs=>fs.AircraftId==id);
        if(isInUse)
        {
            throw new InvalidOperationException("This aircraft is assigned to at least 1 flightSchedule and cannot be deleted.");
        }
        _context.Aircrafts.Remove(aircraft);
        await _context.SaveChangesAsync();
        return true;
    }
}