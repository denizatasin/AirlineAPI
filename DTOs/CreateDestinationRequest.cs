using System.ComponentModel.DataAnnotations;

namespace AirlineAPI.DTOs;

public class CreateDestinationRequest
{
    [Required(ErrorMessage ="City is required.")]
    public string City{get;set;}="";
    [Required(ErrorMessage ="RangeStart is required.")]
    public int RangeStart{get;set;}
    [Required(ErrorMessage ="RangeEnd is required.")]
    public int RangeEnd{get;set;}
}