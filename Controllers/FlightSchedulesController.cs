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

    public FlightSchedulesController(IFlightScheduleService flightScheduleService)
    {
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
                return Conflict($"Flight schedule with Flight number'{flightSchedule.FlightNumber}' already exists.");
            }
            var createdSchedule=await _flightScheduleService.GetFlightScheduleByIdAsync(flightSchedule.Id);
            return CreatedAtAction(nameof(GetFlightSchedule),new{id=flightSchedule.Id},createdSchedule!.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
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
                return NotFound();
            }
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFlightSchedule(int id)
    {
        bool deleted=await _flightScheduleService.DeleteFlightScheduleAsync(id);
        if(!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}