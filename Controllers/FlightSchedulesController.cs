using AirlineAPI.Models;
using AirlineAPI.DTOs;
using AirlineAPI.Services;
using AirlineAPI.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightSchedulesController : ControllerBase
{
    private readonly IFlightScheduleService _flightScheduleService;
    private readonly ILogger<FlightSchedulesController> _logger;
    public FlightSchedulesController(IFlightScheduleService flightScheduleService,ILogger<FlightSchedulesController> logger)
    {
        _logger=logger;
        _flightScheduleService=flightScheduleService;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<FlightScheduleResponse>>> GetAllFlightSchedules()
    {
        var flightSchedules=await _flightScheduleService.GetAllFlightSchedulesAsync();
        var response=flightSchedules.Select(flightSchedule=>flightSchedule.ToResponse()).ToList();
        return Ok(response);
    }
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<FlightScheduleResponse>> GetFlightSchedule(int id)
    {
        var flightSchedule=await _flightScheduleService.GetFlightScheduleByIdAsync(id);
        if(flightSchedule==null)
        {
            _logger.LogWarning("FlightSchedule ID {FlightScheduleId} not found.",id);
            return NotFound();
        }
        return Ok(flightSchedule.ToResponse());
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FlightScheduleResponse>> AddFlightSchedule(CreateFlightScheduleRequest request)
    {
        try
        {
            var flightSchedule=request.ToEntity();
            bool added=await _flightScheduleService.AddFlightScheduleAsync(flightSchedule);
            if(!added)
            {
                _logger.LogWarning("Flight schedule creation failed. Flight number '{FlightNumber}' already exists.",flightSchedule.FlightNumber);
                return Conflict($"Flight schedule with Flight number'{flightSchedule.FlightNumber}' already exists.");
            }
            var createdSchedule=await _flightScheduleService.GetFlightScheduleByIdAsync(flightSchedule.Id);
            _logger.LogInformation("FlightSchedule ID {FlightScheduleId} created via HTTP POST.",flightSchedule.Id);
            return CreatedAtAction(nameof(GetFlightSchedule),new{id=flightSchedule.Id},createdSchedule!.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to create flight schedule '{FlightNumber}'.",request.FlightNumber);
            return Conflict(ex.Message);
        }
        
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFlightSchedule(int id,UpdateFlightScheduleRequest request)
    {
        try
        {
            var flightSchedule=request.ToEntity();
            bool updated=await _flightScheduleService.UpdateFlightScheduleAsync(id,flightSchedule);
            if(!updated)
            {
                _logger.LogWarning("FlightSchedule update failed. ID {FlightScheduleId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("FlightSchedule ID {FlightScheduleId} updated via HTTP PUT.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to update flight schedule ID {FlightScheduleId}.",id);
            return Conflict(ex.Message);
        }
        
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFlightSchedule(int id)
    {
        try
        {
            bool deleted=await _flightScheduleService.DeleteFlightScheduleAsync(id);
            if(!deleted)
            {
                _logger.LogWarning("FlightSchedule deletion failed. ID {FlightScheduleId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("FlightSchedule ID {FlightScheduleId} deleted via HTTP DELETE.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to delete flight schedule ID {FlightScheduleId}.",id);
            return Conflict(ex.Message);
        }
        
    }
}