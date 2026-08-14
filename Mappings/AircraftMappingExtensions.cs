using AirlineAPI.Models;
using AirlineAPI.DTOs;

namespace AirlineAPI.Mappings;

public static class AircraftMappingExtensions
{
    public static AircraftResponse ToResponse(this Aircraft aircraft)
    {
        return new AircraftResponse
        {
            Id=aircraft.Id,
            Manufacturer=aircraft.Manufacturer,
            Model=aircraft.Model,
            Capacity=aircraft.Capacity,
            TailNumber=aircraft.TailNumber
        };
    }
    public static Aircraft ToEntity(this CreateAircraftRequest request)
    {
        return new Aircraft
        {
            Manufacturer=request.Manufacturer,
            Model=request.Model,
            Capacity=request.Capacity,
            TailNumber=request.TailNumber
        };
    }
    public static Aircraft ToEntity(this UpdateAircraftRequest request)
    {
        return new Aircraft
        {
            Manufacturer=request.Manufacturer,
            Model=request.Model,
            Capacity=request.Capacity,
            TailNumber=request.TailNumber
        };
    }
}