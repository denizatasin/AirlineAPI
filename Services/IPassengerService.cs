using AirlineAPI.DTOs;
using AirlineAPI.Models;

namespace AirlineAPI.Services;

public interface IPassengerService
{
    Task<List<Passenger>> GetAllPassengersAsync();
    Task<Passenger?> GetPassengerByIdAsync(int id);
    Task<bool> AddPassengerAsync(Passenger passenger);
    Task<bool> UpdatePassengerAsync(int id, Passenger updatedPassenger);
    Task<bool> DeletePassengerAsync(int id);
    Task<Passenger?> TopUpBalanceAsync(int id, decimal amount);
    Task<bool> SetBalanceAsync(int id, decimal balance);
    Task<PassengerStats?> GetStatsAsync(int id);
    Task<Passenger?> UpdateMyProfileAsync(int id, UpdateMyProfileRequest request);
}
/// <summary>A passenger together with the numbers shown on the profile page.</summary>
public class PassengerStats
{
    public Passenger Passenger{get;set;}=null!;
    public int TotalFlights{get;set;}
    public int ActiveTickets{get;set;}
    public decimal TotalSpent{get;set;}
}
