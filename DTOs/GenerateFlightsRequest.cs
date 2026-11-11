using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

public class GenerateFlightsRequest
{
    [Range(1,30,ErrorMessage ="Days must be between 1 and 30.")]
    public int Days{get;set;}=10;
}