using AirlineAPI.Models;
using AirlineAPI.DTOs;

namespace AirlineAPI.Mappings;

public static class FlightScheduleMappingExtensions
{
    private static TimeSpan CalculateDuration(TimeSpan departureTime, TimeSpan arrivalTime)
    {
        var duration=arrivalTime-departureTime;
        if(duration<TimeSpan.Zero)
        {
            duration+=TimeSpan.FromHours(24);
        }
        return duration;
    }
    public static FlightScheduleResponse ToResponse(this FlightSchedule schedule)
    {
        return new FlightScheduleResponse
        {
            Id=schedule.Id,
            FlightNumber=schedule.FlightNumber,
            Departure=schedule.Departure,
            Arrival=schedule.Arrival,
            DepartureTime=schedule.DepartureTime,
            ArrivalTime=schedule.ArrivalTime,
            Duration=schedule.Duration,
            Price=schedule.Price,
            City=schedule.Destination.City,
            Aircraft=schedule.Aircraft.ToResponse()
        };
    }
    public static FlightSchedule ToEntity(this CreateFlightScheduleRequest request)
    {
        return new FlightSchedule
        {
            FlightNumber=request.FlightNumber,
            Departure=request.Departure,
            Arrival=request.Arrival,
            DepartureTime=request.DepartureTime,
            ArrivalTime=request.ArrivalTime,
            Duration=CalculateDuration(request.DepartureTime,request.ArrivalTime),
            Price=request.Price,
            AircraftId=request.AircraftId
        };
    }
    public static FlightSchedule ToEntity(this UpdateFlightScheduleRequest request)
    {
        return new FlightSchedule
        {
            FlightNumber=request.FlightNumber,
            Departure=request.Departure,
            Arrival=request.Arrival,
            DepartureTime=request.DepartureTime,
            ArrivalTime=request.ArrivalTime,
            Duration=CalculateDuration(request.DepartureTime,request.ArrivalTime),
            Price=request.Price,
            AircraftId=request.AircraftId
        };
    }
}