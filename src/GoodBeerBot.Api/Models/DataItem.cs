namespace GoodBeerBot.Api.Models;

public class DataItem
{
    public string Name { get; set; }

    public int Left { get; set; }

    public int Days {  get; set; }

    public DateOnly Expiry { get; set; }

    public DataItem(string name, int left, int days, DateOnly expiry)
    {
        Name = name;
        Left = left;
        Days = days;
        Expiry = expiry;
    }
}