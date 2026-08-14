using AirlineAPI.DTOs;
using AirlineAPI.Models;
using AirlineAPI.Mappings;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController: ControllerBase
{
    private readonly IDestinationService _destinationService;
    public DestinationsController(IDestinationService destinationService)
    {
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
            return CreatedAtAction(nameof(GetDestination),new{id=destination.Id},destination.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
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
                return NotFound();
            }
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
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