using AirlineAPI.DTOs;
using AirlineAPI.Models;

namespace AirlineAPI.Mappings;

public static class TicketMappingExtensions
{
    public static TicketResponse ToResponse(this Ticket ticket)
    {
        return new TicketResponse
        {
            Id=ticket.Id,
            TicketNumber=ticket.TicketNumber,
            Status=ticket.Status.ToString(),
            PricePaid=ticket.PricePaid,
            PurchaseDate=ticket.PurchaseDate,
            Passenger=ticket.Passenger.ToResponse(),
            Flight=ticket.Flight.ToResponse()
        };
    }
    public static Ticket ToEntity(this CreateTicketRequest request)
    {
        return new Ticket
        {
            PassengerId=request.PassengerId,
            FlightId=request.FlightId,
            PricePaid=request.PricePaid,
            PurchaseDate=DateOnly.FromDateTime(DateTime.Now)
        };
    }
}