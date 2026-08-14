using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface ITicketService
{
    Task<List<Ticket>>GetAllTicketsAsync();
    Task<Ticket?> GetTicketByIdAsync(int id);
    Task<bool> AddTicketAsync(Ticket ticket);
    Task<bool> UpdateTicketAsync(int id, TicketStatus newStatus);
    Task<bool> DeleteTicketAsync(int id);
}