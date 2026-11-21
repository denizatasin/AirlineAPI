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
    public async Task<ActionResult<PassengerProfileResponse>> GetMyProfile()
    {
        var currentPassengerId = User.GetPassengerId();
        if (currentPassengerId == null)
        {
            _logger.LogWarning("Unauthorized access to passenger profile. No PassengerId claim found.");
            return Unauthorized();
        }
        var stats = await _passengerService.GetStatsAsync(currentPassengerId.Value);
        if (stats == null)
        {
            _logger.LogWarning("Passenger profile not found for PassengerId {PassengerId}",currentPassengerId);
            return NotFound();
        }
        return Ok(stats.Passenger.ToProfileResponse(stats));
    }
    [Authorize(Roles = "Passenger")]
    [HttpGet("me/miles")]
    public async Task<ActionResult<List<MilesTransactionResponse>>> GetMyMiles()
    {
        var currentPassengerId = User.GetPassengerId();
        if (currentPassengerId == null)
        {
            return Unauthorized();
        }
        var history = await _passengerService.GetMilesHistoryAsync(currentPassengerId.Value);
        return Ok(history.Select(m => new MilesTransactionResponse
        {
            Id = m.Id,
            Amount = m.Amount,
            Type = m.Type,
            Description = m.Description,
            CreatedAt = m.CreatedAt
        }).ToList());
    }
    [Authorize(Roles = "Passenger")]
    [HttpPut("me")]
    public async Task<ActionResult<PassengerProfileResponse>> UpdateMyProfile(UpdateMyProfileRequest request)
    {
        var currentPassengerId = User.GetPassengerId();
        if (currentPassengerId == null)
        {
            return Unauthorized();
        }
        try
        {
            var passenger = await _passengerService.UpdateMyProfileAsync(currentPassengerId.Value, request);
            if (passenger == null)
            {
                return NotFound();
            }
            var stats = await _passengerService.GetStatsAsync(passenger.Id);
            return Ok(passenger.ToProfileResponse(stats!));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
    [Authorize(Roles = "Passenger")]
    [HttpPost("me/balance")]
    public async Task<ActionResult<PassengerResponse>> TopUpMyBalance(TopUpRequest request)
    {
        var currentPassengerId = User.GetPassengerId();
        if (currentPassengerId == null)
        {
            return Unauthorized();
        }
        try
        {
            var passenger = await _passengerService.TopUpBalanceAsync(currentPassengerId.Value, request.Amount);
            if (passenger == null)
            {
                return NotFound();
            }
            return Ok(passenger.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
    [Authorize(Roles="Admin")]
    [HttpPut("{id}/balance")]
    public async Task<IActionResult> SetBalance(int id,SetBalanceRequest request)
    {
        try
        {
            bool updated=await _passengerService.SetBalanceAsync(id,request.Balance);
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
    [Authorize(Roles="Admin")]
    [HttpGet]
    public async Task<ActionResult<List<PassengerDetailResponse>>> GetAllPassengers()
    {
        var passengers=await _passengerService.GetAllPassengersAsync();
        var usernames=await _passengerService.GetUsernamesAsync();
        var response=passengers.Select(passenger=>passenger.ToDetailResponse(usernames.GetValueOrDefault(passenger.Id))).ToList();
        return Ok(response);
    }
    [Authorize(Roles="Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<PassengerDetailResponse>> GetPassenger(int id)
    {
        var passenger=await _passengerService.GetPassengerByIdAsync(id);
        if(passenger==null)
        {
            _logger.LogWarning("Passenger ID {PassengerId} not found.",id);
            return NotFound();
        }
        var allUsernames=await _passengerService.GetUsernamesAsync();
        return Ok(passenger.ToDetailResponse(allUsernames.GetValueOrDefault(passenger.Id)));
    }
    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<ActionResult<PassengerDetailResponse>> AddPassenger(CreatePassengerRequest request)
    {
        var passenger=request.ToEntity();
        await _passengerService.AddPassengerAsync(passenger);
        _logger.LogInformation("Passenger ID {PassengerId} created via HTTP POST.",passenger.Id);
        return CreatedAtAction(nameof(GetPassenger),new{id=passenger.Id},passenger.ToDetailResponse());
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