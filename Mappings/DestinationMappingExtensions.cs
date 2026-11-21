using AirlineAPI.DTOs;
using AirlineAPI.Models;

namespace AirlineAPI.Mappings;

public static class DestinationMappingExtensions
{
    public static DestinationResponse ToResponse(this Destination destination)
    {
        return new DestinationResponse
        {
            Id=destination.Id,
            City=destination.City,
            RangeStart=destination.RangeStart,
            RangeEnd=destination.RangeEnd,
            DistanceKm=destination.DistanceKm
        };
    }
    public static Destination ToEntity(this CreateDestinationRequest request)
    {
        return new Destination
        {
            City=request.City,
            RangeStart=request.RangeStart,
            RangeEnd=request.RangeEnd,
            DistanceKm=request.DistanceKm
        };
    }
    public static Destination ToEntity(this UpdateDestinationRequest request)
    {
        return new Destination
        {
            City=request.City,
            RangeStart=request.RangeStart,
            RangeEnd=request.RangeEnd,
            DistanceKm=request.DistanceKm
        };
    }
}