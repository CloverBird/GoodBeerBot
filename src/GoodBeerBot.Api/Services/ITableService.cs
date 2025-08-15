using GoodBeerBot.Api.Models;

namespace GoodBeerBot.Api.Services;

public interface ITableService
{
    Task<List<DataItem>> GetDataItemsAsync();

    Task<List<Position>> ReadPositionsAsync();

    Task SavePositionsToSheetAsync(long chatId, IEnumerable<Position> positions);

    Task AppendReportRowAsync(DateTime when, long chatId, string point, string name, DateOnly? expiry, int qty);
}