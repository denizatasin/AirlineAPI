namespace AirlineAPI.DTOs;

public class PassengerResponse
{
    public int Id{get;set;}
    public string FirstName{get;set;}="";
    public string LastName{get;set;}="";
    public decimal Balance{get;set;}
    public int Miles{get;set;}
}
/// <summary>Full passenger data. Only for the admin and the passenger themselves (never nested in tickets).</summary>
public class PassengerDetailResponse: PassengerResponse
{
    public string? Email{get;set;}
    public string? IdentityNumber{get;set;}
    public string? Gender{get;set;}
    public string? Nationality{get;set;}
    public string? Phone{get;set;}
    public DateOnly? BirthDate{get;set;}
    /// <summary>Login name of the passenger's account (null if the passenger has no account). Never the password.</summary>
    public string? Username{get;set;}
}
/// <summary>The passenger's own profile: identity number is masked and statistics are added.</summary>
public class PassengerProfileResponse: PassengerDetailResponse
{
    /// <summary>Flights already flown (tickets whose flight has departed and was not cancelled).</summary>
    public int TotalFlights{get;set;}
    /// <summary>Upcoming tickets (Booked / CheckedIn).</summary>
    public int ActiveTickets{get;set;}
    /// <summary>Total of PricePaid over all tickets that were not cancelled.</summary>
    public decimal TotalSpent{get;set;}
    /// <summary>Classic, Silver, Gold or Diamond.</summary>
    public string Tier{get;set;}="Classic";
    /// <summary>Miles earned from flights in the last 12 months.</summary>
    public int StatusMiles{get;set;}
    public string? NextTier{get;set;}
    /// <summary>Status miles needed in total for the next level.</summary>
    public int? NextTierMiles{get;set;}
}