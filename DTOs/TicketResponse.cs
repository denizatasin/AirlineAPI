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
    /// <summary>Miles spent on this ticket as a discount.</summary>
    public int MilesUsed{get;set;}
    /// <summary>First / Business / Economy (null if the seat is unknown).</summary>
    public string? Cabin{get;set;}
    /// <summary>When online check-in opens (departure minus 24 hours).</summary>
    public DateTime CheckInOpensAt{get;set;}
    public string Gate{get;set;}=""; 
    public TimeSpan BoardingTime{get;set;}
    public PassengerResponse Passenger{get;set;}=null!;
    public FlightResponse Flight{get;set;}=null!;
}