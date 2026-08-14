using AirlineAPI.Models;
using AirlineAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Services;

public class DestinationService:IDestinationService
{
    private readonly AirlineDbContext _context;
    public DestinationService(AirlineDbContext context)
    {
        _context=context;
    }
    private static bool IsValidRange(int rangeStart,int rangeEnd, out string? errorMessage)
    {
        errorMessage=null;
        if(rangeStart>=rangeEnd)
        {
            errorMessage=$"Range start ({rangeStart}) must be less than range end ({rangeEnd}).";
            return false;
        }
        int numbers=rangeStart-rangeEnd+1;
        if(numbers%2!=0)
        {
            errorMessage=$"The range {rangeStart}-{rangeEnd} must contain an even number of flights so every departure has a return flight.";
            return false;
        }        
        return true;
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public async Task<List<Destination>>GetAllDestinationsAsync()
    {
        return await _context.Destinations.ToListAsync();
    }
    public async Task<Destination?> GetDestinationByIdAsync(int id)
    {
        return await _context.Destinations.FindAsync(id);
    }
    public async Task<bool> AddDestinationAsync(Destination destination)
    {
        if(!IsValidRange(destination.RangeStart,destination.RangeEnd,out string? rangeError))
        {
            throw new InvalidOperationException(rangeError);
        }
        _context.Destinations.Add(destination);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateDestinationAsync(int id,Destination updatedDestination)
    {
        var existingDestination=await _context.Destinations.FirstOrDefaultAsync(d=>d.Id==id);
        if(existingDestination==null)
        {
            return false;
        }
        if(!IsValidRange(updatedDestination.RangeStart,updatedDestination.RangeEnd,out string? rangeError))
        {
            throw new InvalidOperationException(rangeError);
        }
        existingDestination.City=updatedDestination.City;
        existingDestination.RangeStart=updatedDestination.RangeStart;
        existingDestination.RangeEnd=updatedDestination.RangeEnd;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteDestinationAsync(int id)
    {
        var destination=await _context.Destinations.FindAsync(id);
        if(destination==null)
        {
            return false;
        }
        bool isInUse=await _context.FlightSchedules.AnyAsync(fs=>fs.DestinationId==id);
        if(isInUse)
        {
            throw new InvalidOperationException("This destination is assigned to one or more flight schedules and cannot be deleted.");
        }
        await _context.SaveChangesAsync();
        return true;
    }
}