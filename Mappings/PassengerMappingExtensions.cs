using AirlineAPI.Models;
using AirlineAPI.DTOs;

namespace AirlineAPI.Mappings;

public static class PassengerMappingExtensions
{
    public static PassengerResponse ToResponse(this Passenger passenger)
    {
        return new PassengerResponse
        {
            Id=passenger.Id,
            FirstName=passenger.FirstName,
            LastName=passenger.LastName
        };
    }
    public static Passenger ToEntity(this CreatePassengerRequest request)
    {
        return new Passenger
        {
            FirstName=request.FirstName,
            LastName=request.LastName
        };
    }
    public static Passenger ToEntity(this UpdatePassengerRequest request)
    {
        return new Passenger
        {
            FirstName=request.FirstName,
            LastName=request.LastName
        };
    }
}