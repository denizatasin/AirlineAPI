using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.Models;

/// <summary>One line of a passenger's miles history (earned, spent or refunded).</summary>
public class MilesTransaction
{
    public int Id{get;set;}
    public int PassengerId{get;set;}
    public Passenger Passenger{get;set;}=null!;
    /// <summary>Positive = miles added, negative = miles spent.</summary>
    public int Amount{get;set;}
    /// <summary>Earned, Spent or Refunded.</summary>
    [MaxLength(20)]
    public string Type{get;set;}="";
    [MaxLength(200)]
    public string Description{get;set;}="";
    public DateTime CreatedAt{get;set;}
}
