using System.Security.Claims;

namespace AirlineAPI.Mappings;

public static class ClaimsPrincipalExtensions
{
    public static bool isAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }
    public static int? GetPassengerId(this ClaimsPrincipal user)
    {
        var claim=user.FindFirst("PassengerId");
        return claim!=null && int.TryParse(claim.Value, out int id) ? id:null;
    }
    public static bool OwnsPassenger(this ClaimsPrincipal user, int passengerId)
    {
        var myPassengerId=user.GetPassengerId();
        return myPassengerId.HasValue && myPassengerId.Value==passengerId; 
    }
}