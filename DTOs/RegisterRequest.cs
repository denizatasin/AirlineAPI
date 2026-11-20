using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

/// <summary>Public registration. Always creates a Passenger account (admins are created by an admin only).</summary>
public class RegisterRequest
{
    [Required(ErrorMessage ="Username is required.")]
    [RegularExpression(@"^[A-Za-z0-9_.]{3,30}$",ErrorMessage ="Username must be 3-30 characters: letters, digits, '_' or '.'.")]
    public string Username{get;set;}="";
    [Required(ErrorMessage ="Password is required.")]
    [StringLength(100,MinimumLength=6,ErrorMessage ="Password must be at least 6 characters.")]
    public string Password{get;set;}="";
    [Required(ErrorMessage ="First name is required.")]
    [RegularExpression(@"^[A-ZÇĞİÖŞÜ][a-zçğıöşü]*(\s[A-ZÇĞİÖŞÜ][a-zçğıöşü]*)*$",ErrorMessage ="First name must contain words starting with an uppercase letter.")]
    public string FirstName{get;set;}="";
    [Required(ErrorMessage ="Last name is required.")]
    [RegularExpression(@"^[A-ZÇĞİÖŞÜ]+$", ErrorMessage = "Last name must contain only uppercase letters.")]
    public string LastName{get;set;}="";
    [Required(ErrorMessage ="Email is required.")]
    public string Email{get;set;}="";
    [Required(ErrorMessage ="Nationality is required.")]
    public string Nationality{get;set;}="";
    [Required(ErrorMessage ="TC kimlik / passport number is required.")]
    public string IdentityNumber{get;set;}="";
    [Required(ErrorMessage ="Gender is required.")]
    public string Gender{get;set;}="";
    public string? Phone{get;set;}
    public DateOnly? BirthDate{get;set;}
}
