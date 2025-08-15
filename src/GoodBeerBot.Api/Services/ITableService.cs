using GoodBeerBot.Api.Models;

namespace GodBeerBot.Api.Services;

public interface ITableService
{
    Task<List<DataItem>> GetDataItemsAsync();

    Task<List<Position>> ReadPositionsAsync();

    Task SavePositionsToSheetAsync(long chatId, IEnumerable<Position> positions);
}