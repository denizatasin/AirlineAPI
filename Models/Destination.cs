namespace AirlineAPI.Models;

public class Destination
{
    public int Id{get;set;}
    public string City{get;set;}="";
    public int RangeStart{get;set;}
    public int RangeEnd{get;set;}
    /// <summary>Approximate distance from Istanbul in km (used for miles).</summary>
    public int DistanceKm{get;set;}
}