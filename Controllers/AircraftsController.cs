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
    private readonly ILogger<AircraftsController> _logger;
    public AircraftsController(IAircraftService aircraftService,ILogger<AircraftsController> logger)
    {
        _logger=logger;
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
            _logger.LogWarning("Aircraft with ID {AircraftId} was not found.",id);
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
                _logger.LogWarning("Failed to add aircraft. Tail number '{TailNumber}' already exists.",aircraft.TailNumber);
                return Conflict($"Aircraft with Tail number '{aircraft.TailNumber}' already exists.");
            }
            _logger.LogInformation("Aircraft with ID {AircraftId} created via HTTP POST.",aircraft.Id);
            return CreatedAtAction(nameof(GetAircraft),new{id=aircraft.Id}, aircraft.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Invalid operation during aircraft addition.");
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
                _logger.LogWarning("Aircraft update failed. Aircraft ID {AircraftId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("Aircraft ID {AircraftId} updated via HTTP PUT.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Invalid operation during aircraft update for ID {AircraftId}.",id);
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
                _logger.LogWarning("Aircraft deletion failed. Aircraft ID {AircraftId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("Aircraft ID {AircraftId} deleted via HTTP DELETE.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Invalid operation during aircraft deletion for ID {AircraftId}.",id);
            return Conflict(ex.Message);
        }
    }
}