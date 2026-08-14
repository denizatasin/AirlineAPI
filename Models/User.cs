namespace AirlineAPI.Models;

public enum UserRole
{
    Admin,
    Passenger
}

public class User
{
    public int Id{get;set;}
    public string Username{get;set;}="";
    public string PasswordHash{get;set;}="";
    public UserRole Role{get;set;}
    public int? PassengerId{get;set;}
    public Passenger? Passenger{get;set;}
}