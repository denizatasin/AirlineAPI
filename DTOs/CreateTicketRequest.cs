using System.ComponentModel.DataAnnotations;

namespace AirlineAPI.DTOs;

public class CreateTicketRequest
{
    [Required(ErrorMessage ="Passenger Id is required.")]
    public int PassengerId{get;set;}
    [Required(ErrorMessage ="Flight Id is required.")]
    public int FlightId{get;set;}
    [Range(0.01,100000,ErrorMessage ="Price Paid must be a positive number and lower than 100000.")]
    public decimal PricePaid{get;set;}
    /// <summary>Optional. Leave empty to get a random free seat.</summary>
    [StringLength(4)]
    public string? SeatNumber{get;set;}
    /// <summary>Optional. Miles to spend as a discount (100 miles = 10 TL, at most 50% of the price).</summary>
    [Range(0,10000000,ErrorMessage ="Miles must be zero or a positive number.")]
    public int MilesToUse{get;set;}
}