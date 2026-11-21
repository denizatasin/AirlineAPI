using AirlineAPI.Data;
using AirlineAPI.DTOs;
using AirlineAPI.Models;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles="Admin")]
public class AdminController:ControllerBase
{
    private readonly AirlineDbContext _context;
    public AdminController(AirlineDbContext context)
    {
        _context=context;
    }
    // Numbers for the admin dashboard. "Revenue" = price paid of tickets that are not cancelled.
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>>GetDashboard([FromQuery]int days=30)
    {
        days=Math.Clamp(days,7,90);
        var today=DateOnly.FromDateTime(DateTime.Now);
        var since=today.AddDays(-(days-1));

        var tickets=await _context.Tickets.AsNoTracking()
            .Select(t=>new
            {
                t.Status,t.PricePaid,t.PurchaseDate,t.SeatNumber,t.FlightId,
                FlightDate=t.Flight.Date,
                t.Flight.FlightSchedule.FlightNumber,
                t.Flight.FlightSchedule.Departure,
                t.Flight.FlightSchedule.Arrival,
                Model=t.Flight.FlightSchedule.Aircraft.Model,
                Capacity=t.Flight.FlightSchedule.Aircraft.Capacity
            }).ToListAsync();
        var sold=tickets.Where(t=>t.Status!=TicketStatus.Cancelled).ToList();

        var response=new DashboardResponse
        {
            PeriodDays=days,
            TotalRevenue=sold.Sum(t=>t.PricePaid),
            ActiveTickets=tickets.Count(t=>t.Status==TicketStatus.Booked||t.Status==TicketStatus.CheckedIn),
            CancelledTickets=tickets.Count(t=>t.Status==TicketStatus.Cancelled),
            UsedTickets=tickets.Count(t=>t.Status==TicketStatus.Used),
            PassengerCount=await _context.Passengers.CountAsync(),
            TotalPassengerBalance=await _context.Passengers.SumAsync(p=>p.Balance),
            TotalMilesOutstanding=await _context.Passengers.SumAsync(p=>(long)p.Miles)
        };

        var inPeriod=sold.Where(t=>t.PurchaseDate>=since).ToList();
        response.RevenueInPeriod=inPeriod.Sum(t=>t.PricePaid);
        response.TicketsInPeriod=inPeriod.Count;
        // every day of the period appears, even with 0 sales, so the chart has no gaps
        for(int i=0;i<days;i++)
        {
            var d=since.AddDays(i);
            var day=inPeriod.Where(t=>t.PurchaseDate==d).ToList();
            response.DailySales.Add(new DailySalesItem{Date=d,Tickets=day.Count,Revenue=day.Sum(t=>t.PricePaid)});
        }

        response.TopRoutes=sold.GroupBy(t=>new{t.FlightNumber,t.Departure,t.Arrival})
            .Select(g=>new RouteStatItem{FlightNumber=g.Key.FlightNumber,Route=$"{g.Key.Departure} → {g.Key.Arrival}",Tickets=g.Count(),Revenue=g.Sum(t=>t.PricePaid)})
            .OrderByDescending(r=>r.Tickets).ThenByDescending(r=>r.Revenue).Take(5).ToList();

        // Cabin mix: the cabin of a seat comes from the aircraft's seat layout
        var layouts=new Dictionary<(string,int),List<CabinSection>>();
        var cabinCounts=new Dictionary<string,int>{["First"]=0,["Business"]=0,["Economy"]=0};
        foreach(var t in sold.Where(t=>t.SeatNumber!=null))
        {
            var key=(t.Model,t.Capacity);
            if(!layouts.TryGetValue(key,out var layout))
            {
                layout=SeatLayoutBuilder.Build(t.Model,t.Capacity);
                layouts[key]=layout;
            }
            string cabin=SeatLayoutBuilder.CabinOfSeat(layout,t.SeatNumber!)??"Economy";
            cabinCounts[cabin]=cabinCounts.GetValueOrDefault(cabin)+1;
        }
        response.Cabins=cabinCounts.Where(c=>c.Value>0).Select(c=>new CabinStatItem{Cabin=c.Key,Tickets=c.Value}).ToList();

        // Upcoming flights: occupancy and the fullest ones
        var flights=await _context.Flights.AsNoTracking().Where(f=>f.Date>=today)
            .Select(f=>new{f.Id,f.Date,f.FlightSchedule.FlightNumber,f.FlightSchedule.Departure,f.FlightSchedule.Arrival,f.FlightSchedule.Aircraft.Capacity})
            .ToListAsync();
        var soldByFlight=sold.GroupBy(t=>t.FlightId).ToDictionary(g=>g.Key,g=>g.Count());
        response.UpcomingFlights=flights.Count;
        int totalSeats=flights.Sum(f=>f.Capacity);
        int soldSeats=flights.Sum(f=>soldByFlight.GetValueOrDefault(f.Id));
        response.UpcomingOccupancy=totalSeats==0?0:Math.Round(soldSeats*100.0/totalSeats,1);
        response.FullestFlights=flights.Where(f=>soldByFlight.GetValueOrDefault(f.Id)>0)
            .Select(f=>new FullFlightItem
            {
                FlightId=f.Id,FlightNumber=f.FlightNumber,Route=$"{f.Departure} → {f.Arrival}",Date=f.Date,
                Sold=soldByFlight[f.Id],Capacity=f.Capacity,Percent=Math.Round(soldByFlight[f.Id]*100.0/f.Capacity,1)
            })
            .OrderByDescending(f=>f.Percent).ThenBy(f=>f.Date).Take(5).ToList();
        return Ok(response);
    }
}
