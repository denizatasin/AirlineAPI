using AirlineAPI.Models;

namespace AirlineAPI.Services;

/// <summary>Online check-in rules and boarding-pass details.</summary>
public static class CheckInRules
{
    /// <summary>Check-in opens this many hours before departure.</summary>
    public const int OpensHoursBefore=24;
    /// <summary>Boarding starts this many minutes before departure.</summary>
    public const int BoardingMinutesBefore=40;

    public static DateTime DepartureOf(Flight flight)=>flight.Date.ToDateTime(TimeOnly.FromTimeSpan(flight.FlightSchedule.DepartureTime));
    public static DateTime OpensAt(Flight flight)=>DepartureOf(flight).AddHours(-OpensHoursBefore);
    public static TimeSpan BoardingTime(Flight flight)=>flight.FlightSchedule.DepartureTime.Subtract(TimeSpan.FromMinutes(BoardingMinutesBefore));

    /// <summary>Same flight number always gives the same gate (e.g. "TK2001" -> "B14"). Nothing is stored in the database.</summary>
    public static string GateFor(string flightNumber)
    {
        int hash=0;
        foreach(char c in flightNumber)
        {
            hash=(hash*31+c)%10007;
        }
        char terminal=(char)('A'+hash%4);
        int number=1+(hash/4)%24;
        return $"{terminal}{number}";
    }
    /// <summary>Returns an error message if the ticket cannot be checked in right now, otherwise null.</summary>
    public static string? WhyNotNow(Ticket ticket,DateTime now)
    {
        if(ticket.Status==TicketStatus.CheckedIn)
        {
            return "Bu biletin check-in'i zaten yapılmış.";
        }
        if(ticket.Status!=TicketStatus.Booked)
        {
            return $"Sadece onaylı (Booked) bilette check-in yapılabilir (bu biletin durumu: {ticket.Status}).";
        }
        if(DepartureOf(ticket.Flight)<=now)
        {
            return "Check-in kapandı: uçuş zaten kalktı.";
        }
        var opens=OpensAt(ticket.Flight);
        if(now<opens)
        {
            return $"Check-in kalkıştan {OpensHoursBefore} saat önce açılır ({opens:dd.MM.yyyy HH:mm}).";
        }
        return null;
    }
}
