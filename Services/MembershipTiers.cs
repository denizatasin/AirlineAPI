namespace AirlineAPI.Services;

/// <summary>
/// Membership levels, modelled after THY Miles&amp;Smiles (Classic, Classic Plus, Elite, Elite Plus) and scaled to this project.
/// The level depends on the miles EARNED FROM FLIGHTS in the last 12 months (spending miles never lowers it).
/// </summary>
public static class MembershipTiers
{
    public const int SilverMiles=10000;
    public const int GoldMiles=30000;
    public const int DiamondMiles=60000;

    public static DateTime WindowStart()
    {
        return DateTime.Now.AddMonths(-12);
    }
    public static string TierFor(int statusMiles)
    {
        if(statusMiles>=DiamondMiles) return "Diamond";
        if(statusMiles>=GoldMiles) return "Gold";
        if(statusMiles>=SilverMiles) return "Silver";
        return "Classic";
    }
    /// <summary>Extra miles earned on every flight, in percent of the flight's miles.</summary>
    public static int BonusPercent(string tier)
    {
        return tier switch
        {
            "Silver"=>10,
            "Gold"=>25,
            "Diamond"=>50,
            _=>0
        };
    }
    /// <summary>The next level and the miles needed for it (null at the top level).</summary>
    public static (string? name,int? miles) NextTier(string tier)
    {
        return tier switch
        {
            "Classic"=>("Silver",SilverMiles),
            "Silver"=>("Gold",GoldMiles),
            "Gold"=>("Diamond",DiamondMiles),
            _=>(null,null)
        };
    }
}
