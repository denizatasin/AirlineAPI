using AirlineAPI.DTOs;
using AirlineAPI.Models;
using AirlineAPI.Mappings;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController: ControllerBase
{
    private readonly IDestinationService _destinationService;
    private readonly ILogger<DestinationsController> _logger;
    public DestinationsController(IDestinationService destinationService,ILogger<DestinationsController> logger)
    {
        _logger=logger;
        _destinationService=destinationService;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<DestinationResponse>>> GetAllDestinations()
    {
        var destinations=await _destinationService.GetAllDestinationsAsync();
        var response=destinations.Select(destination=>destination.ToResponse()).ToList();
        return Ok(response);
    }
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<DestinationResponse>>GetDestination(int id)
    {
        var destination=await _destinationService.GetDestinationByIdAsync(id);
        if(destination==null)
        {
            _logger.LogWarning("Destination ID {DestinationId} not found.",id);
            return NotFound();
        }
        return Ok(destination.ToResponse());
    }
    [Authorize(Roles ="Admin")]
    [HttpPost]
    public async Task<ActionResult<DestinationResponse>> AddDestination(CreateDestinationRequest request)
    {
        try
        {
            var destination=request.ToEntity();
            await _destinationService.AddDestinationAsync(destination);
            _logger.LogInformation("Destination ID {DestionationId} ({City}) created via HTTP POST.",destination.Id,destination.City);
            return CreatedAtAction(nameof(GetDestination),new{id=destination.Id},destination.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to create destination '{City}'.",request.City);
            return Conflict(ex.Message);
        }
    }
    [Authorize(Roles ="Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDestination(int id, UpdateDestinationRequest request)
    {
        try
        {
            var destination=request.ToEntity();
            bool updated=await _destinationService.UpdateDestinationAsync(id,destination);
            if(!updated)
            {
                _logger.LogWarning("Destination update failed. ID {DestinationId} not found.",id);
                return NotFound();
            }
            _logger.LogInformation("Destination ID {DestinationId} updated via HTTP PUT.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to update destination ID {DestinationId}.",id);
            return Conflict(ex.Message);
        }
    }
    [Authorize(Roles ="Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDestination(int id)
    {
        try
        {
            bool deleted=await _destinationService.DeleteDestinationAsync(id);
            if(!deleted)
            {
                _logger.LogWarning("Destination deletion failed. ID {DestinationId} not found.",id);
                return NotFound();
            }   
            _logger.LogInformation("Destination ID {DestinationId} deleted via HTTP DELETE.",id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogWarning(ex,"Failed to delete destination ID {DestinationId}.",id);
            return Conflict(ex.Message);
        }
    }
}