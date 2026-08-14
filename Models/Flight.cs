namespace AirlineAPI.Models;

/// <summary>
/// Represents a specific occurence of a flight schedule on a given date.
/// </summary>
public class Flight
{
    public int Id{get;set;}
    public DateOnly Date{get;set;}
    //foreign key
    public int FlightScheduleId{get;set;}
    //navigation property
    public FlightSchedule FlightSchedule{get;set;}=null!;
}