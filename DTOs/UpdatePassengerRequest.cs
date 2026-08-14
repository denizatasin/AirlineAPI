using System.ComponentModel.DataAnnotations;

namespace AirlineAPI.DTOs;

public class UpdatePassengerRequest
{
    [Required(ErrorMessage ="First name is required.")]
    [RegularExpression(@"^[A-ZÇĞİÖŞÜ][a-zçğiöşü]*(\s[A-ZÇĞİÖŞÜ][a-zçğiöşü]*)*$",ErrorMessage ="First name must contain words starting with an uppercase letter.")]
    public string FirstName{get;set;}="";
    [Required(ErrorMessage ="Last name is required.")]
    [RegularExpression(@"^[A-ZÇĞİÖŞÜ]+$", ErrorMessage = "Last name must contain only uppercase letters.")]
    public string LastName{get;set;}="";
}