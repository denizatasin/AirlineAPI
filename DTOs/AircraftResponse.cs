namespace AirlineAPI.DTOs;

public class AircraftResponse
{
    public int Id{get;set;}
    public string Manufacturer{get;set;}="";
    public string Model{get;set;}="";
    public int Capacity{get;set;}
    public string TailNumber{get;set;}="";
}