namespace AirlineAPI.DTOs;

public class MilesTransactionResponse
{
    public int Id{get;set;}
    public int Amount{get;set;}
    public string Type{get;set;}="";
    public string Description{get;set;}="";
    public DateTime CreatedAt{get;set;}
}
