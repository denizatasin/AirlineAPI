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
    public PassengersController(IPassengerService passengerService)
    {
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
        if (currentPassengerId == null) return Unauthorized();

        var passenger = await _passengerService.GetPassengerByIdAsync(currentPassengerId.Value);
        if (passenger == null) return NotFound();

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
            return NotFound();
        }
        return NoContent();
    }
    [Authorize(Roles="Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePassenger(int id)
    {
        bool deleted=await _passengerService.DeletePassengerAsync(id);
        if(!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}