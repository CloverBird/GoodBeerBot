namespace GoodBeerBot.Api.Models;

public class UserState
{
    public string? Point { get; set; }  // GB1-10
    public int Index { get; set; }           
    public List<Position> Positions { get; set; } = new();
    public List<(string Name, DateOnly Expiry, int Qty)> Answers { get; set; } = new();
}