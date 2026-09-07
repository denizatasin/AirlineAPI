using AirlineAPI.Services;
using AirlineAPI.DTOs;
using AirlineAPI.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassengersController: ControllerBase
{
    private readonly IPassengerService _passengerService;
    private readonly ILogger<PassengersController> _logger;
    public PassengersController(IPassengerService passengerService,ILogger<PassengersController> logger)
    {
        _logger=logger;
        _passengerService=passengerService;
    }
    private int? GetCurrentPassengerId()
    {
        var userIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("PassengerId")?.Value;
        return int.TryParse(userIdClaim, out int id) ? id : null;
    }
    [Authorize(Roles = "Passenger")]
    [HttpGet("me")]
    public async Task<ActionResult<PassengerResponse>> GetMyProfile()
    {
        var currentPassengerId = User.GetPassengerId();
        if (currentPassengerId == null)
        {
            _logger.LogWarning("Unauthorized access to passenger profile. No PassengerId claim found.");
            return Unauthorized();
        } 

        var passenger = await _passengerService.GetPassengerByIdAsync(currentPassengerId.Value);
        if (passenger == null)
        {
            _logger.LogWarning("Passenger profile not found for PassengerId {PassengerId}",currentPassengerId);
            return NotFound();
        } 

        return Ok(passenger.ToResponse());
    }
    [Authorize(Roles="Admin")]
    [HttpGet]
    public async Task<ActionResult<List<PassengerResponse>>> GetAllPassengers()
    {
        var passengers=await _passengerService.GetAllPassengersAsync();
        var response=passengers.Select(passenger=>passenger.ToResponse()).ToList();
        return Ok(response);
    }
    [Authorize(Roles="Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<PassengerResponse>> GetPassenger(int id)
    {
        var passenger=await _passengerService.GetPassengerByIdAsync(id);
        if(passenger==null)
        {
            _logger.LogWarning("Passenger ID {PassengerId} not found.",id);
            return NotFound();
        }
        return Ok(passenger.ToResponse());
    }
    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<ActionResult<PassengerResponse>> AddPassenger(CreatePassengerRequest request)
    {
        var passenger=request.ToEntity();
        await _passengerService.AddPassengerAsync(passenger);
        _logger.LogInformation("Passenger ID {PassengerId} created via HTTP POST.",passenger.Id);
        return CreatedAtAction(nameof(GetPassenger),new{id=passenger.Id},passenger.ToResponse());
    }
    [Authorize(Roles="Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePassenger(int id,UpdatePassengerRequest request)
    {
        var passenger=request.ToEntity();
        bool updated=await _passengerService.UpdatePassengerAsync(id,passenger);
        if(!updated)
        {
            _logger.LogWarning("Passenger update failed. ID {PassengerId} not found.",id);
            return NotFound();
        }
        _logger.LogInformation("Passenger ID {PassengerId} updated via HTTP PUT.",id);
        return NoContent();
    }
    [Authorize(Roles="Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePassenger(int id)
    {
        bool deleted=await _passengerService.DeletePassengerAsync(id);
        if(!deleted)
        {
            _logger.LogWarning("Passenger deletion failed. ID {PassengerId} not found.", id);
            return NotFound();
        }
        _logger.LogInformation("Passenger ID {PassengerId} deleted via HTTP DELETE.", id);
        return NoContent();
    }
}