namespace AirlineAPI.Models;

/// <summary>
/// Represents an aircraft used in flights.
/// </summary>
public class Aircraft
{
    public int Id{get;set;}
    public string Manufacturer{get;set;}="";
    public string Model{get;set;}="";
    public int Capacity{get;set;}
    public string TailNumber{get;set;}="";
    //collection navigation property
    public ICollection<FlightSchedule> FlightSchedules{get;set;}=new List<FlightSchedule>();

}