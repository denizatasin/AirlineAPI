namespace AirlineAPI.DTOs;

public class TicketResponse
{
    public int Id{get;set;}
    public string TicketNumber{get;set;}="";
    public string Status{get;set;}="";
    public decimal PricePaid{get;set;}
    public DateOnly PurchaseDate{get;set;}
    public string? SeatNumber{get;set;}
    /// <summary>True if the price was paid from the passenger's balance (cancelling refunds it).</summary>
    public bool PaidFromBalance{get;set;}
    public PassengerResponse Passenger{get;set;}=null!;
    public FlightResponse Flight{get;set;}=null!;
}