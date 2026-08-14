namespace AirlineAPI.DTOs;

/// <summary>
/// Represents the flight information returned to the client.
/// Only contains data that the API wants to expose.
/// </summary>
public class FlightResponse
{
    public int Id{get;set;}
    public DateOnly Date{get;set;}
    public string FlightNumber{get;set;}="";
    public string Departure{get;set;}="";
    public string Arrival{get;set;}="";
    public decimal Price{get;set;}
    public TimeSpan DepartureTime{get;set;}
    public TimeSpan ArrivalTime{get;set;}
}