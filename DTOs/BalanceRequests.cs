using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

/// <summary>A passenger adds money to their own balance.</summary>
public class TopUpRequest
{
    [Range(1,50000,ErrorMessage ="Amount must be between 1 and 50,000 TL.")]
    public decimal Amount{get;set;}
}

/// <summary>An admin sets a passenger's balance to an exact value.</summary>
public class SetBalanceRequest
{
    [Range(0,1000000,ErrorMessage ="Balance must be between 0 and 1,000,000 TL.")]
    public decimal Balance{get;set;}
}
