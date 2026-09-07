using AirlineAPI.Data;
using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class AircraftService : IAircraftService
{
    private readonly AirlineDbContext _context;
    private readonly ILogger<AircraftService> _logger;
    public AircraftService(AirlineDbContext context, ILogger<AircraftService> logger)
    {
        _logger=logger;
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
        var aircraft= await _context.Aircrafts.FindAsync(id);
        if(aircraft==null)
        {
            _logger.LogWarning("Aircraft query failed. Aircraft ID {AircraftId} not found.", id);
        }
        return aircraft;
    }
    public async Task<bool> AddAircraftAsync(Aircraft aircraft)
    {
        if(!IsValidManufacturer(aircraft.Manufacturer))
        {
            _logger.LogWarning("Aircraft addition failed. Invalid manufacturer: {Manufacturer}", aircraft.Manufacturer);
            throw new InvalidOperationException($"Invalid manufacturer '{aircraft.Manufacturer}'. Only 'Airbus' and 'Boeing' are accepted."); 
        }
        if(!IsValidModel(aircraft.Manufacturer,aircraft.Model))
        {
            string validModelsMessage = ValidModelsByManufacturer.TryGetValue(aircraft.Manufacturer, out var validModels)? string.Join(", ", validModels) : "unknown manufacturer";
            _logger.LogWarning("Aircraft addition failed. Invalid model '{Model}' for manufacturer '{Manufacturer}'", aircraft.Model, aircraft.Manufacturer);
            throw new InvalidOperationException($"Invalid model '{aircraft.Model}' for manufacturer '{aircraft.Manufacturer}'. Proper models for {aircraft.Manufacturer}: {validModelsMessage}");
        }
        bool exists=await _context.Aircrafts.AnyAsync(a=>a.TailNumber==aircraft.TailNumber);
        if(exists)
        {
            _logger.LogWarning("Aircraft addition failed. Tail number {TailNumber} already exists.", aircraft.TailNumber);
            return false;
        }
        _context.Aircrafts.Add(aircraft);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Aircraft added successfully. ID: {AircraftId}, TailNumber: {TailNumber}, Model: {Model}", aircraft.Id, aircraft.TailNumber, aircraft.Model);
        return true;
    }
    public async Task<bool> UpdateAircraftAsync(int id, Aircraft updatedAircraft)
    {
        var existingAircraft=await _context.Aircrafts.FirstOrDefaultAsync(a=>a.Id==id);
        if(existingAircraft==null)
        {
            _logger.LogWarning("Aircraft update failed. Aircraft ID {AircraftId} not found.", id);
            return false;
        }
        if(!IsValidManufacturer(updatedAircraft.Manufacturer))
        {
            _logger.LogWarning("Aircraft update failed. Invalid manufacturer: {Manufacturer}", updatedAircraft.Manufacturer);
            throw new InvalidOperationException($"Invalid manufacturer '{updatedAircraft.Manufacturer}'. Only 'Airbus' and 'Boeing' are accepted."); 
        } 
        if(!IsValidModel(updatedAircraft.Manufacturer,updatedAircraft.Model))
        {
            _logger.LogWarning("Aircraft update failed. Invalid model '{Model}' for manufacturer '{Manufacturer}'", updatedAircraft.Model, updatedAircraft.Manufacturer);
            throw new InvalidOperationException($"Invalid model '{updatedAircraft.Model}' for manufacturer '{updatedAircraft.Manufacturer}'.");
        } 
        existingAircraft.Manufacturer=updatedAircraft.Manufacturer;
        existingAircraft.Model=updatedAircraft.Model;
        existingAircraft.Capacity=updatedAircraft.Capacity;
        existingAircraft.TailNumber=updatedAircraft.TailNumber;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Aircarft ID {AircraftId} updated successfully. TailNumber: {TailNumber}", id, existingAircraft.TailNumber);
        return true;
    }
    public async Task<bool> DeleteAircraftAsync(int id)
    {
        var aircraft=await _context.Aircrafts.FindAsync(id);
        if(aircraft==null)
        {
            _logger.LogWarning("Aircraft deletion failed. Aircaft ID {AircarftId} not found.", id);
            return false;
        }
        bool isInUse=await _context.FlightSchedules.AnyAsync(fs=>fs.AircraftId==id);
        if(isInUse)
        {
            _logger.LogWarning("Aircraft deletion failed. Aircraft ID {AircraftId} is assigned to a flight schedule.", id);
            throw new InvalidOperationException("This aircraft is assigned to at least 1 flightSchedule and cannot be deleted.");
        }
        _context.Aircrafts.Remove(aircraft);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Aircraft ID {AircraftId} (TailNumber: {TailNumber}) deleted.", id, aircraft.TailNumber);
        return true;
    }
}