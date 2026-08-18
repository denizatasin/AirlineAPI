using System.ComponentModel.DataAnnotations;

namespace AirlineAPI.DTOs;

public class CreateDestinationRequest
{
    [Required(ErrorMessage ="City is required.")]
    [RegularExpression(@"^[A-ZÇĞİÖŞÜ][a-zçğıöşü]*(\s[A-ZÇĞİÖŞÜ][a-zçğıöşü]*)*$",ErrorMessage ="City name must start with an uppercase letter followed by lowercase letters.")]
    public string City{get;set;}="";
    [Required(ErrorMessage ="RangeStart is required.")]
    public int RangeStart{get;set;}
    [Required(ErrorMessage ="RangeEnd is required.")]
    public int RangeEnd{get;set;}
}