namespace AirlineAPI.DTOs;

public class FlightScheduleResponse
{
    public int Id{get;set;}
    public string FlightNumber{get;set;}="";
    public string Departure{get;set;}="";
    public string Arrival{get;set;}="";
    public TimeSpan DepartureTime{get;set;}
    public TimeSpan ArrivalTime{get;set;}
    public TimeSpan Duration{get;set;}
    public decimal Price{get;set;}
    public string City{get;set;}="";
    public AircraftResponse Aircraft{get;set;}=null!;
}