using AirlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using AirlineAPI.Models;
using AirlineAPI.DTOs;
using AirlineAPI.Mappings;
using Microsoft.AspNetCore.Authorization;

namespace AirlineAPI.Controllers;

/// <summary>
/// Handles HTTP requests related to flights.
/// Receives requests from clients and delegates business logic to the service layer.
/// </summary>

[ApiController]//attribute that this class is an API controller
[Route("api/[controller]")]//attribute that defines the route for this controller
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightsController> _logger;
    public FlightsController(IFlightService flightService,ILogger<FlightsController> logger)
    {
        _logger=logger;
        _flightService=flightService;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<FlightResponse>>> GetAllFlights()
    {
        var flights= await _flightService.GetAllFlightsAsync();
        var response=flights.Select(flight=>flight.ToResponse()).ToList();
        return Ok(response);
    }
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<FlightResponse>> GetFlight(int id)
    {
        var flight=await _flightService.GetFlightByIdAsync(id);
        if(flight==null)
        {
            _logger.LogWarning("Flight ID {FlightId} not found.",id);
            return NotFound();
        }
        var response=flight.ToResponse();
        return Ok(response);
    }
    [Authorize(Roles ="Admin")]
    [HttpPost]
    public async Task<ActionResult<FlightResponse>> AddFlight(CreateFlightRequest request)
    {
        try
        {
            var flight=request.ToEntity();
            bool added=await _flightService.AddFlightAsync(flight);
            if(!added)
            {
                _logger.LogWarning("Flight creation failed. Schedule ID {FlightScheduleId} on Date {Date} already exists.",flight.FlightScheduleId,flight.Date);
                return Conflict($"FlightScheduleId '{flight.FlightScheduleId}' and Date '{flight.Date}' already exists.");
            }
            var createdFlight=await _flightService.GetFlightByIdAsync(flight.Id);
            _logger.LogInformation("Flight ID {FlightId} created via HTTP POST.",flight.Id);
            return CreatedAtAction(nameof(GetFlight),new {id=flight.Id},createdFlight!.ToResponse()); 
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to create flight for flight schedule ID {FlightScheduleId}.",request.FlightScheduleId);
            return Conflict(ex.Message);
        }
        
    }
    [Authorize(Roles ="Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFlight(int id, UpdateFlightRequest request)
    {
        try
        {
            bool updated=await _flightService.UpdateFlightAsync(id,request.Date);
            if(!updated)
            {
                _logger.LogWarning("Flight update failed. Flight ID {FlightId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("Flight ID {FlightId} updated via HTTP PUT.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to update flight ID {FlightId}.",id);
            return Conflict(ex.Message);
        }
        
    }
    [Authorize(Roles ="Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFlight(int id)
    {
        try
        {
            bool deleted=await _flightService.DeleteFlightAsync(id);
            if(!deleted)
            {
                _logger.LogWarning("Flight deletion failed. Flight ID {FlightId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("Fligth ID {FlightId} deleted via HTTP DELETE.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to delete fligt ID {FlightId}.",id);
            return Conflict(ex.Message);
        }
    }
}