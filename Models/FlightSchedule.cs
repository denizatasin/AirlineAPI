namespace AirlineAPI.Models;
///<summary>
/// Represents a fixed flight route/schedule
/// including its departure/arrival info and assigned aircraft.
/// </summary>
public class FlightSchedule
{
    public int Id{get;set;}
    public string FlightNumber {get;set;}="";
    public string Departure {get;set;}="";
    public string Arrival {get;set;}="";
    public TimeSpan DepartureTime {get;set;}
    public TimeSpan ArrivalTime {get;set;}
    public TimeSpan Duration {get;set;}
    public decimal Price{get;set;}
    //foreign key
    public int AircraftId{get;set;}
    public int DestinationId{get;set;}
    //navigation property
    public Aircraft Aircraft{get;set;}=null!;
    public Destination Destination{get;set;}=null!;
}