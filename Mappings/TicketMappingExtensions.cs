using AirlineAPI.DTOs;
using AirlineAPI.Models;
using AirlineAPI.Services;

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
            SeatNumber=ticket.SeatNumber,
            PaidFromBalance=ticket.BalanceCharged,
            MilesUsed=ticket.MilesUsed,
            Cabin=CabinOf(ticket),
            CheckInOpensAt=CheckInRules.OpensAt(ticket.Flight),
            Gate=CheckInRules.GateFor(ticket.Flight.FlightSchedule.FlightNumber),
            BoardingTime=CheckInRules.BoardingTime(ticket.Flight),
            Passenger=ticket.Passenger.ToResponse(),
            Flight=ticket.Flight.ToResponse()
        };
    }
    private static string? CabinOf(Ticket ticket)
    {
        var aircraft=ticket.Flight.FlightSchedule.Aircraft;
        if(ticket.SeatNumber==null || aircraft==null)
        {
            return null;
        }
        return SeatLayoutBuilder.CabinOfSeat(SeatLayoutBuilder.Build(aircraft.Model,aircraft.Capacity),ticket.SeatNumber);
    }
    public static Ticket ToEntity(this CreateTicketRequest request)
    {
        return new Ticket
        {
            PassengerId=request.PassengerId,
            FlightId=request.FlightId,
            PricePaid=request.PricePaid,
            SeatNumber=string.IsNullOrWhiteSpace(request.SeatNumber) ? null : request.SeatNumber.Trim().ToUpperInvariant(),
            MilesUsed=request.MilesToUse,
            PurchaseDate=DateOnly.FromDateTime(DateTime.Now)
        };
    }
}