using AirlineAPI.DTOs;
using AirlineAPI.Models;

namespace AirlineAPI.Mappings;

/// <summary>
/// Contains extension methods for converting between
/// Flight entities and Flight-related DTOs.
/// Centralizes mapping logic to keep controllers and services clean.
/// </summary>
public static class FlightMappingExtensions
{
    public static FlightResponse ToResponse(this Flight flight,int? availableSeats=null)
    {
        return new FlightResponse
        {
            Id=flight.Id,
            Date=flight.Date,
            FlightNumber=flight.FlightSchedule.FlightNumber,
            Departure=flight.FlightSchedule.Departure,
            Arrival=flight.FlightSchedule.Arrival,
            Price=flight.FlightSchedule.Price,
            DepartureTime=flight.FlightSchedule.DepartureTime,
            ArrivalTime=flight.FlightSchedule.ArrivalTime,
            AircraftModel=flight.FlightSchedule.Aircraft?.Model ?? "",
            AvailableSeats=availableSeats
        };
    }
    public static Flight ToEntity(this CreateFlightRequest request)
    {
        return new Flight
        {
            FlightScheduleId=request.FlightScheduleId,
            Date=request.Date
        };
    }
}