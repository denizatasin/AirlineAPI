namespace AirlineAPI.Models;

public enum TicketStatus
{
    Booked,     //0
    CheckedIn,  //1
    Cancelled,  //2
    Used        //3
}
/// <summary>
/// Represents a ticket purchased by a passenger for a specific flight occurrence.
/// </summary>
public class Ticket
{
    public int Id{get;set;}
    public string TicketNumber{get;set;}="";
    public TicketStatus Status{get;set;}
    public decimal PricePaid{get;set;}
    public DateOnly PurchaseDate{get;set;}
    /// <summary>Seat such as "12A". Null for tickets created before seat selection existed.</summary>
    public string? SeatNumber{get;set;}
    /// <summary>True if the price was taken from the passenger's balance (so cancelling refunds it).</summary>
    public bool BalanceCharged{get;set;}
    /// <summary>Miles spent on this ticket as a discount (0 = none).</summary>
    public int MilesUsed{get;set;}
    /// <summary>True once the miles of this flight were given to the passenger (so they are never given twice).</summary>
    public bool MilesAwarded{get;set;}
    //foreign keys
    public int PassengerId{get;set;}
    public int FlightId{get;set;}
    //navigation properties
    public Passenger Passenger{get;set;}=null!;
    public Flight Flight{get;set;}=null!;
}