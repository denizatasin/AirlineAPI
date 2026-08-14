using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

public class CreateFlightScheduleRequest
{
    [Required(ErrorMessage="Flight number is required.")]
    [RegularExpression(@"^[A-Z]{2,3}[0-9]{1,4}$",ErrorMessage="Flight number must start with 2-3 uppercase letters followed by 1-4 digits.")]
    public string FlightNumber{get;set;}="";
    [Required(ErrorMessage ="Departure is required.")]
    [RegularExpression(@"^[A-Z][a-z]{2,}(?: [A-Z][a-z]{2,})*$", ErrorMessage = "Departure must start with an uppercase letter followed by lowercase letters; words may be separated by spaces.")]
    public string Departure{get;set;}="";
    [Required(ErrorMessage ="Arrival is required.")]
    [RegularExpression(@"^[A-Z][a-z]{2,}(?: [A-Z][a-z]{2,})*$", ErrorMessage = "Arrival must start with an uppercase letter followed by lowercase letters; words may be separated by spaces.")]
    public string Arrival{get;set;}="";
    public TimeSpan DepartureTime{get;set;}
    public TimeSpan ArrivalTime{get;set;}
    [Range(0.01,100000,ErrorMessage ="Price must be between 0-100000.")]
    public decimal Price{get;set;}
    [Required(ErrorMessage ="Aircraft id is required.")]
    public int AircraftId{get;set;}
}