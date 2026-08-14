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
}