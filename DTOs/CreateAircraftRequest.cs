using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

public class CreateAircraftRequest
{
    [Required(ErrorMessage ="Manufacturer is required.")]
    public string Manufacturer{get;set;}="";
    [Required(ErrorMessage ="Model is required.")]
    public string Model{get;set;}="";
    [Required(ErrorMessage ="Capacity is required.")]
    [Range(50,800,ErrorMessage ="Capacity must be between 50 and 800.")]
    public int Capacity{get;set;}
    [Required(ErrorMessage ="Tail number is required.")]
    [RegularExpression(@"^[A-Z]{1,2}-[A-Z]{3}$",ErrorMessage ="Tail number must be in a valid format(e.g. TC-JDM).")]
    public string TailNumber{get;set;}="";
}