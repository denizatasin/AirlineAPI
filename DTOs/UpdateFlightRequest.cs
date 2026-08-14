using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

public class UpdateFlightRequest
{
    [Required(ErrorMessage="Date is required.")]
    public DateOnly Date{get;set;}
}