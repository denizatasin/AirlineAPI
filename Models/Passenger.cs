namespace AirlineAPI.Models;
///<summary>
/// Represent a passenger who can purchase tickets.
/// </summary>
public class Passenger
{
    public int Id{get;set;}
    public string FirstName{get;set;}="";
    public string LastName{get;set;}="";
}