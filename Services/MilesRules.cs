namespace AirlineAPI.Services;

/// <summary>All the numbers of the miles system in one place. Change them here to tune the system.</summary>
public static class MilesRules
{
    /// <summary>Every flight gives at least this many miles (before the cabin multiplier), even short domestic ones.</summary>
    public const int MinimumMiles=500;
    /// <summary>Value of one mile in TL: 100 miles = 10 TL.</summary>
    public const decimal LiraPerMile=0.10m;
    /// <summary>At most this share of a ticket price can be paid with miles.</summary>
    public const decimal MaxDiscountRatio=0.5m;

    /// <summary>Miles earned for a flight: distance (at least MinimumMiles) x cabin multiplier (Economy 1, Business 1.5, First 2).</summary>
    public static int Earned(int distanceKm,string cabinName)
    {
        return (int)Math.Round(Math.Max(MinimumMiles,distanceKm)*SeatLayoutBuilder.MultiplierOf(cabinName),MidpointRounding.AwayFromZero);
    }
    /// <summary>The most miles that may be used on a ticket of this price.</summary>
    public static int MaxUsable(decimal price,int availableMiles)
    {
        int limitByPrice=(int)Math.Floor(price*MaxDiscountRatio/LiraPerMile);
        return Math.Max(0,Math.Min(availableMiles,limitByPrice));
    }
    public static decimal Discount(int miles)
    {
        return miles*LiraPerMile;
    }
}
