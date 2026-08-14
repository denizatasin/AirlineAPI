using AirlineAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AirlineAPI.Data;
/// <summary>
/// Represents the application's database session.
/// Responsible for managing entity sets and communicating with SQL Server.
/// </summary>
public class AirlineDbContext: DbContext
{
    public AirlineDbContext(DbContextOptions<AirlineDbContext> options): base(options)
    {
    }
    public DbSet<Flight> Flights { get; set;}=null!;
    public DbSet<FlightSchedule> FlightSchedules{get;set;}=null!;
    public DbSet<Aircraft> Aircrafts{get;set;}=null!;
    public DbSet<Passenger> Passengers{get;set;}=null!;
    public DbSet<Ticket> Tickets{get;set;}=null!;
    public DbSet<Destination> Destinations{get;set;}=null!;
    public DbSet<User> Users{get;set;}=null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aircraft>().HasIndex(a=>a.TailNumber).IsUnique();
        modelBuilder.Entity<Flight>().HasIndex(f=>new{f.FlightScheduleId,f.Date}).IsUnique();
        modelBuilder.Entity<Destination>().HasData
        (
            new Destination { Id = 1, City = "New York", RangeStart = 1, RangeEnd = 4 },
            new Destination { Id = 2, City = "Los Angeles", RangeStart = 7, RangeEnd = 10 },
            new Destination { Id = 3, City = "Washington", RangeStart = 11, RangeEnd = 14 },
            new Destination { Id = 4, City = "Tokyo", RangeStart = 50, RangeEnd = 53 },
            new Destination { Id = 5, City = "Dubai", RangeStart = 760, RangeEnd = 763 },
            new Destination { Id = 6, City = "Berlin", RangeStart = 1720, RangeEnd = 1725 },
            new Destination { Id = 7, City = "Paris", RangeStart = 1820, RangeEnd = 1827 },
            new Destination { Id = 8, City = "Roma", RangeStart = 1860, RangeEnd = 1865 },
            new Destination { Id = 9, City = "Amsterdam", RangeStart = 1950, RangeEnd = 1955 },
            new Destination { Id = 10, City = "Londra", RangeStart = 1980, RangeEnd = 1987 },
            new Destination { Id = 11, City = "Ankara", RangeStart = 2100, RangeEnd = 2111 },
            new Destination { Id = 12, City = "Izmir", RangeStart = 2300, RangeEnd = 2311 },
            new Destination { Id = 13, City = "Antalya", RangeStart = 2400, RangeEnd = 2413 },
            new Destination { Id = 14, City = "Adana", RangeStart = 2450, RangeEnd = 2455 },
            new Destination { Id = 15, City = "Bodrum", RangeStart = 2500, RangeEnd = 2513 },
            new Destination { Id = 16, City = "Trabzon", RangeStart = 2820, RangeEnd = 2825 }
        );
        modelBuilder.Entity<User>().HasIndex(u=>u.Username).IsUnique();
    }
}