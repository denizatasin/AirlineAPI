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
    //foreign keys
    public int PassengerId{get;set;}
    public int FlightId{get;set;}
    //navigation properties
    public Passenger Passenger{get;set;}=null!;
    public Flight Flight{get;set;}=null!;
}