using AirlineAPI.DTOs;
using AirlineAPI.Mappings;
using AirlineAPI.Models;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]

public class TicketsController:ControllerBase
{
    private readonly ITicketService _ticketService;
    public TicketsController(ITicketService ticketService)
    {
        _ticketService=ticketService;
    } 
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<TicketResponse>>> GetAllTickets()
    {
        var tickets=await _ticketService.GetAllTicketsAsync();
        if(!User.isAdmin())
        {
            var myPassengerId=User.GetPassengerId();
            if(myPassengerId==null)
            {
                return StatusCode(403, "Your account is not linked to a passenger profile.");
            }
            tickets=tickets.Where(t=>t.PassengerId==myPassengerId.Value).ToList();
        }
        var response=tickets.Select(ticket=>ticket.ToResponse()).ToList();
        return Ok(response);
    }
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketResponse>> GetTicket(int id)
    {
        var ticket=await _ticketService.GetTicketByIdAsync(id);
        if(ticket==null)
        {
            return NotFound();
        }
        
        if(!User.isAdmin() && !User.OwnsPassenger(ticket.PassengerId))
        {
            return StatusCode(403, "You can only view your own tickets.");
        }
        return Ok(ticket.ToResponse());
    }
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> AddTicket(CreateTicketRequest request)
    {
        
        if(!User.isAdmin() && !User.OwnsPassenger(request.PassengerId))
        {
            return StatusCode(403, "You can only purchase tickets for yourself.");
        }
        try
        {
            var ticket=request.ToEntity();
            await _ticketService.AddTicketAsync(ticket);
            var createdTicket=await _ticketService.GetTicketByIdAsync(ticket.Id);
            return CreatedAtAction(nameof(GetTicket),new {id=ticket.Id},createdTicket!.ToResponse());
        }
        catch(InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        
    }
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id,UpdateTicketRequest request)
    {
        var existingTicket=await _ticketService.GetTicketByIdAsync(id);
        if(existingTicket==null)
        {
            return NotFound();
        }
        if(!User.isAdmin())
        {
            
            if(!User.OwnsPassenger(existingTicket.PassengerId))
            {
                return StatusCode(403, "You can only cancel your own tickets.");
            }
            if(request.Status!=TicketStatus.Cancelled)
            {
                return StatusCode(403, "Passengers can only cancel tickets, not change them to other statuses.");
            }
        }
        try
        {
            bool updated=await _ticketService.UpdateTicketAsync(id,request.Status);
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        bool deleted=await _ticketService.DeleteTicketAsync(id);
        if(!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}