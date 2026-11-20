namespace AirlineAPI.Services;

/// <summary>One cabin section (First, Business or Economy) of an aircraft.</summary>
public class CabinSection
{
    public string Name{get;set;}="";
    /// <summary>Seats per block between the aisles, e.g. [2,3,2].</summary>
    public int[] Layout{get;set;}=Array.Empty<int>();
    public int FirstRow{get;set;}
    public int RowCount{get;set;}
    /// <summary>Seat count of the very last row of this cabin (may be smaller than a full row).</summary>
    public int LastRowSeats{get;set;}
    public int SeatsPerRow=>Layout.Sum();
}

/// <summary>One row of seats, e.g. row 12 with seats 12A, 12B, 12C...</summary>
public class SeatRow
{
    public int Number{get;set;}
    public List<string> Seats{get;set;}=new();
}

/// <summary>
/// Builds the cabin layout of an aircraft from its model and capacity.
/// Nothing is stored in the database: the layout is always calculated.
/// </summary>
public static class SeatLayoutBuilder
{
    // Wide-body models (3-3-3 economy). Every other model is treated as narrow-body (3-3).
    private static readonly HashSet<string> WideBodyModels=new()
    {
        "B777-300ER","B787-9 Dreamliner","A330-200","A330-300","A350-900"
    };
    // I is skipped on purpose (it looks like 1)
    private const string Letters="ABCDEFGHJKL";

    // Price of a seat = base (Economy) price x cabin multiplier. Change these to tune the prices.
    public const decimal BusinessMultiplier=1.5m;
    public const decimal FirstMultiplier=2.0m;
    public static bool IsWideBody(string model)=>WideBodyModels.Contains(model);
    public static decimal MultiplierOf(string cabinName)=>cabinName switch
    {
        "First"=>FirstMultiplier,
        "Business"=>BusinessMultiplier,
        _=>1.0m
    };
    public static decimal PriceFor(decimal basePrice,string cabinName)=>Math.Round(basePrice*MultiplierOf(cabinName),2,MidpointRounding.AwayFromZero);
    /// <summary>Name of the cabin a seat belongs to (null if the seat does not exist).</summary>
    public static string? CabinOfSeat(IEnumerable<CabinSection> cabins,string seat)
    {
        foreach(var cabin in cabins)
        {
            if(RowsOf(cabin).Any(r=>r.Seats.Contains(seat)))
            {
                return cabin.Name;
            }
        }
        return null;
    }
    /// <summary>Seat names of the Economy cabin only (used for random assignment).</summary>
    public static List<string> EconomySeatNumbers(IEnumerable<CabinSection> cabins)
    {
        return AllSeatNumbers(cabins.Where(c=>c.Name=="Economy"));
    }

    public static List<CabinSection> Build(string model,int capacity)
    {
        return IsWideBody(model) ? BuildWide(capacity) : BuildNarrow(capacity);
    }
    // First 2-2 (~10%), Business 2-3-2 (~25%), Economy 3-3-3 (rest)
    private static List<CabinSection> BuildWide(int capacity)
    {
        int bestFirst=-1,bestBusiness=-1;
        double bestCost=double.MaxValue;
        for(int f=1;f*4<capacity;f++)
        {
            for(int b=0;f*4+b*7<capacity;b++)
            {
                int rest=capacity-f*4-b*7;
                if(rest%9!=0)
                {
                    continue;
                }
                double cost=Math.Abs(f*4.0/capacity-0.10)+Math.Abs(b*7.0/capacity-0.25);
                if(cost<bestCost)
                {
                    bestCost=cost;bestFirst=f;bestBusiness=b;
                }
            }
        }
        if(bestFirst<0)
        {
            // No exact fit: pick the closest shares and let the last economy row be shorter
            bestFirst=Math.Max(1,(int)Math.Round(capacity*0.10/4));
            bestBusiness=(int)Math.Round(capacity*0.25/7);
        }
        return Assemble(capacity,bestFirst,new[]{2,2},bestBusiness,new[]{2,3,2},new[]{3,3,3});
    }
    // No First, Business 2-2 (~30%), Economy 3-3 (rest)
    private static List<CabinSection> BuildNarrow(int capacity)
    {
        int bestBusiness=-1;
        double bestCost=double.MaxValue;
        for(int b=0;b*4<capacity;b++)
        {
            int rest=capacity-b*4;
            if(rest%6!=0)
            {
                continue;
            }
            double cost=Math.Abs(b*4.0/capacity-0.30);
            if(cost<bestCost)
            {
                bestCost=cost;bestBusiness=b;
            }
        }
        if(bestBusiness<0)
        {
            bestBusiness=(int)Math.Round(capacity*0.30/4);
        }
        return Assemble(capacity,0,new[]{2,2},bestBusiness,new[]{2,2},new[]{3,3});
    }
    private static List<CabinSection> Assemble(int capacity,int firstRows,int[] firstLayout,int businessRows,int[] businessLayout,int[] economyLayout)
    {
        var cabins=new List<CabinSection>();
        int nextRow=1;
        int remaining=capacity;
        void Add(string name,int rows,int[] layout)
        {
            if(rows<=0 || remaining<=0)
            {
                return;
            }
            int perRow=layout.Sum();
            int seats=Math.Min(rows*perRow,remaining);
            int fullRows=(seats+perRow-1)/perRow;
            int lastRowSeats=seats-(fullRows-1)*perRow;
            cabins.Add(new CabinSection{Name=name,Layout=layout,FirstRow=nextRow,RowCount=fullRows,LastRowSeats=lastRowSeats});
            nextRow+=fullRows;
            remaining-=seats;
        }
        Add("First",firstRows,firstLayout);
        Add("Business",businessRows,businessLayout);
        // Economy takes whatever is left (all rows full when an exact fit was found)
        int economyPerRow=economyLayout.Sum();
        Add("Economy",(remaining+economyPerRow-1)/economyPerRow,economyLayout);
        return cabins;
    }
    /// <summary>Rows of one cabin with their seat names (the last row may be shorter).</summary>
    public static List<SeatRow> RowsOf(CabinSection cabin)
    {
        var rows=new List<SeatRow>();
        for(int r=0;r<cabin.RowCount;r++)
        {
            int seatsInRow=r==cabin.RowCount-1 ? cabin.LastRowSeats : cabin.SeatsPerRow;
            var row=new SeatRow{Number=cabin.FirstRow+r};
            for(int i=0;i<seatsInRow;i++)
            {
                row.Seats.Add($"{row.Number}{Letters[i]}");
            }
            rows.Add(row);
        }
        return rows;
    }
    /// <summary>Seat names of the whole aircraft in row order.</summary>
    public static List<string> AllSeatNumbers(IEnumerable<CabinSection> cabins)
    {
        return cabins.SelectMany(RowsOf).SelectMany(row=>row.Seats).ToList();
    }
    /// <summary>
    /// Validates a requested seat, or picks a random free seat when none was requested.
    /// </summary>
    public static (string? seat,string? error) ChooseSeat(IReadOnlyCollection<string> allSeats,IEnumerable<string> takenSeats,string? requested,IReadOnlyCollection<string>? randomPool=null)
    {
        var taken=new HashSet<string>(takenSeats);
        if(!string.IsNullOrWhiteSpace(requested))
        {
            string seat=requested.Trim().ToUpperInvariant();
            if(!allSeats.Contains(seat))
            {
                return (null,$"Seat '{seat}' does not exist on this aircraft.");
            }
            if(taken.Contains(seat))
            {
                return (null,$"Seat '{seat}' is already taken. Please choose another seat.");
            }
            return (seat,null);
        }
        var free=(randomPool??allSeats).Where(s=>!taken.Contains(s)).ToList();
        if(free.Count==0)
        {
            return (null,randomPool!=null ? "There are no free Economy seats left. Please choose a Business or First seat." : "There are no free seats on this flight.");
        }
        return (free[Random.Shared.Next(free.Count)],null);
    }
}
