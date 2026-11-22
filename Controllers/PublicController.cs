using AirlineAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Controllers;

/// <summary>Numbers for the public home page (no login needed, nothing personal).</summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController:ControllerBase
{
    private readonly AirlineDbContext _context;
    public PublicController(AirlineDbContext context)
    {
        _context=context;
    }
    [HttpGet("overview")]
    public async Task<IActionResult>GetOverview()
    {
        var fleet=await _context.Aircrafts.AsNoTracking()
            .GroupBy(a=>a.Model)
            .Select(g=>new{model=g.Key,count=g.Count()})
            .ToListAsync();
        return Ok(new
        {
            // every destination plus the Istanbul hub
            cityCount=await _context.Destinations.CountAsync()+1,
            routeCount=await _context.FlightSchedules.CountAsync(),
            aircraftCount=fleet.Sum(f=>f.count),
            fleet
        });
    }
}
