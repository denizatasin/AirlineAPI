namespace AirlineAPI.DTOs;

public class DashboardResponse
{
    public decimal TotalRevenue{get;set;}
    public decimal RevenueInPeriod{get;set;}
    public int TicketsInPeriod{get;set;}
    public int ActiveTickets{get;set;}
    public int CancelledTickets{get;set;}
    public int UsedTickets{get;set;}
    public int PassengerCount{get;set;}
    public int UpcomingFlights{get;set;}
    /// <summary>Sold seats / total seats on upcoming flights, in percent (0-100).</summary>
    public double UpcomingOccupancy{get;set;}
    public decimal TotalPassengerBalance{get;set;}
    public long TotalMilesOutstanding{get;set;}
    public int PeriodDays{get;set;}
    public List<DailySalesItem> DailySales{get;set;}=new();
    public List<RouteStatItem> TopRoutes{get;set;}=new();
    public List<CabinStatItem> Cabins{get;set;}=new();
    public List<FullFlightItem> FullestFlights{get;set;}=new();
}
public class DailySalesItem{public DateOnly Date{get;set;} public int Tickets{get;set;} public decimal Revenue{get;set;}}
public class RouteStatItem{public string FlightNumber{get;set;}="";public string Route{get;set;}="";public int Tickets{get;set;} public decimal Revenue{get;set;}}
public class CabinStatItem{public string Cabin{get;set;}="";public int Tickets{get;set;}}
public class FullFlightItem{public int FlightId{get;set;} public string FlightNumber{get;set;}="";public string Route{get;set;}="";public DateOnly Date{get;set;} public int Sold{get;set;} public int Capacity{get;set;} public double Percent{get;set;}}
