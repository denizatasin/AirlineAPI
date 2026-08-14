using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

/// <summary>
/// Represents the data required to create a new flight.
/// Only contains fields that the client is allowed to provide.
/// </summary>
public class CreateFlightRequest
{
    [Required(ErrorMessage="FlightScheduleId is required.")]
    public int FlightScheduleId{get;set;}
    [Required(ErrorMessage="Date is required.")]
    public DateOnly Date{get;set;}
}