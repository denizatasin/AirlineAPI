using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Models;
///<summary>
/// Represent a passenger who can purchase tickets.
/// </summary>
public class Passenger
{
    public int Id{get;set;}
    public string FirstName{get;set;}="";
    public string LastName{get;set;}="";
    /// <summary>Money the passenger can spend on tickets (TL). ConcurrencyCheck: two requests cannot spend the same money twice.</summary>
    [Precision(18,2)]
    [ConcurrencyCheck]
    public decimal Balance{get;set;}
    /// <summary>Miles earned from flights (can be spent as a discount on tickets). ConcurrencyCheck: the same miles cannot be spent twice.</summary>
    [ConcurrencyCheck]
    public int Miles{get;set;}
    // Profile information. All nullable because passengers created before this feature have none of it.
    /// <summary>Stored in lower case. Unique when present.</summary>
    [MaxLength(254)]
    public string? Email{get;set;}
    /// <summary>TC kimlik no for Turkish citizens, passport number for foreigners. Unique when present.</summary>
    [MaxLength(20)]
    public string? IdentityNumber{get;set;}
    /// <summary>Female, Male or Other.</summary>
    [MaxLength(20)]
    public string? Gender{get;set;}
    [MaxLength(60)]
    public string? Nationality{get;set;}
    [MaxLength(20)]
    public string? Phone{get;set;}
    public DateOnly? BirthDate{get;set;}
}