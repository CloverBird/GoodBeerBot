namespace GoodBeerBot.Api.Models;

public class Position
{ 
    public string Name { get; set; }

    public DateOnly? Expiry {  get; set; }

    public Position(string name, DateOnly? expiry)
    {
        Name = name;
        Expiry = expiry;
    }
}