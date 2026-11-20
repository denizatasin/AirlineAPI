using AirlineAPI.Models;
using AirlineAPI.DTOs;
using AirlineAPI.Services;

namespace AirlineAPI.Mappings;

public static class PassengerMappingExtensions
{
    public static PassengerResponse ToResponse(this Passenger passenger)
    {
        return new PassengerResponse
        {
            Id=passenger.Id,
            FirstName=passenger.FirstName,
            LastName=passenger.LastName,
            Balance=passenger.Balance
        };
    }
    public static PassengerDetailResponse ToDetailResponse(this Passenger passenger)
    {
        return new PassengerDetailResponse
        {
            Id=passenger.Id,
            FirstName=passenger.FirstName,
            LastName=passenger.LastName,
            Balance=passenger.Balance,
            Email=passenger.Email,
            IdentityNumber=passenger.IdentityNumber,
            Gender=passenger.Gender,
            Nationality=passenger.Nationality,
            Phone=passenger.Phone,
            BirthDate=passenger.BirthDate
        };
    }
    public static PassengerProfileResponse ToProfileResponse(this Passenger passenger,PassengerStats stats)
    {
        return new PassengerProfileResponse
        {
            Id=passenger.Id,
            FirstName=passenger.FirstName,
            LastName=passenger.LastName,
            Balance=passenger.Balance,
            Email=passenger.Email,
            IdentityNumber=PassengerRules.MaskIdentity(passenger.IdentityNumber),
            Gender=passenger.Gender,
            Nationality=passenger.Nationality,
            Phone=passenger.Phone,
            BirthDate=passenger.BirthDate,
            TotalFlights=stats.TotalFlights,
            ActiveTickets=stats.ActiveTickets,
            TotalSpent=stats.TotalSpent
        };
    }
    public static Passenger ToEntity(this CreatePassengerRequest request)
    {
        return new Passenger
        {
            FirstName=request.FirstName,
            LastName=request.LastName,
            Email=PassengerRules.NormalizeEmail(request.Email),
            IdentityNumber=PassengerRules.NormalizeIdentity(request.IdentityNumber),
            Gender=PassengerRules.NormalizeText(request.Gender),
            Nationality=PassengerRules.NormalizeText(request.Nationality),
            Phone=PassengerRules.NormalizePhone(request.Phone),
            BirthDate=request.BirthDate
        };
    }
    public static Passenger ToEntity(this UpdatePassengerRequest request)
    {
        return new Passenger
        {
            FirstName=request.FirstName,
            LastName=request.LastName,
            Email=PassengerRules.NormalizeEmail(request.Email),
            IdentityNumber=PassengerRules.NormalizeIdentity(request.IdentityNumber),
            Gender=PassengerRules.NormalizeText(request.Gender),
            Nationality=PassengerRules.NormalizeText(request.Nationality),
            Phone=PassengerRules.NormalizePhone(request.Phone),
            BirthDate=request.BirthDate
        };
    }
}