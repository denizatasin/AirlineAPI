namespace AirlineAPI.DTOs;

public class SeatRowResponse
{
    public int Number{get;set;}
    public List<string> Seats{get;set;}=new();
}
public class SeatCabinResponse
{
    public string Name{get;set;}="";
    /// <summary>Seats per block between the aisles, e.g. [3,3,3].</summary>
    public int[] Layout{get;set;}=Array.Empty<int>();
    /// <summary>Price of one seat in this cabin.</summary>
    public decimal Price{get;set;}
    public List<SeatRowResponse> Rows{get;set;}=new();
}
public class SeatMapResponse
{
    public int FlightId{get;set;}
    public string FlightNumber{get;set;}="";
    public string Model{get;set;}="";
    public int Capacity{get;set;}
    /// <summary>Economy price of the flight.</summary>
    public decimal BasePrice{get;set;}
    public int AvailableSeats{get;set;}
    public List<SeatCabinResponse> Cabins{get;set;}=new();
    public List<string> TakenSeats{get;set;}=new();
}
