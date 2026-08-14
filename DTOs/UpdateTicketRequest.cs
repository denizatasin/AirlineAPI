using System.ComponentModel.DataAnnotations;
using AirlineAPI.Models;

namespace AirlineAPI.DTOs;

public class UpdateTicketRequest
{
    [Required(ErrorMessage ="Status is required.")]
    public TicketStatus Status{get;set;}
}