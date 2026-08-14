using AirlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using AirlineAPI.Models;
using AirlineAPI.DTOs;
using AirlineAPI.Mappings;
using Microsoft.AspNetCore.Authorization;

namespace AirlineAPI.Controllers;

/// <summary>
/// Handles HTTP requests related to aircraft.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AircraftsController : ControllerBase
{
    private readonly IAircraftService _aircraftService;
    public AircraftsController(IAircraftService aircraftService)
    {
        _aircraftService=aircraftService;
    }
    [HttpGet]
    public async Task<ActionResult<List<AircraftResponse>>> GetAllAircrafts()
    {
        var aircrafts=await _aircraftService.GetAllAircraftsAsync();
        var response=aircrafts.Select(aircraft=>aircraft.ToResponse()).ToList();
        return Ok(response);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<AircraftResponse>> GetAircraft(int id)
    {
        var aircraft=await _aircraftService.GetAircraftByIdAsync(id);
        if(aircraft==null)
        {
            return NotFound();
        }
        return Ok(aircraft.ToResponse());
    }
    [HttpPost]
    public async Task<ActionResult<AircraftResponse>> AddAircraft(CreateAircraftRequest request)
    {
        try
        {
            var aircraft=request.ToEntity();
            bool added=await _aircraftService.AddAircraftAsync(aircraft);
            if(!added)
            {
                return Conflict($"Aircraft with Tail number '{aircraft.TailNumber}' already exists.");
            }
            return CreatedAtAction(nameof(GetAircraft),new{id=aircraft.Id}, aircraft.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAircraft(int id,UpdateAircraftRequest request)
    {
        try
        {
            var aircraft=request.ToEntity();
            bool updated=await _aircraftService.UpdateAircraftAsync(id,aircraft);
            if(!updated)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAircraft(int id)
    {
        try
        {
            bool deleted=await _aircraftService.DeleteAircraftAsync(id);
            if(!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}