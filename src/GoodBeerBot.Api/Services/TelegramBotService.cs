using GoodBeerBot.Api.Configurations;
using GoodBeerBot.Api.Models;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoodBeerBot.Api.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _bot;
    private readonly ITableService _tables;
    private readonly ILogger<TelegramBotService> _log;

    private readonly long _adminChatId;   
    private readonly long _employeeChatId;


    private static readonly Dictionary<long, UserState> _states = new();

    private static readonly Regex OstatkiCmd = new(@"^/ostatki_gb(\d{1,2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TelegramBotService(
        ITelegramBotClient bot,
        ITableService tables,
        ILogger<TelegramBotService> log,
        TelegramBotConfiguration telegramBotConfiguration)
    {
        _bot = bot;
        _tables = tables;
        _log = log;

        _adminChatId = telegramBotConfiguration.AdminChatId;
        _employeeChatId = telegramBotConfiguration.EmployeeChatId;
    }

    public async Task ProcessUpdateAsync(Update update)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null)
            return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text.Trim();

        // 1) /start
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            _states.Remove(chatId);
            await SendTextAsync(chatId, "Введите команду для выбора точки магазина (например, /ostatki_gb1)");
            return;
        }

        // 2) ручний запуск сповіщень адміну/співробітнику (зручно для тесту)
        if (text.Equals("/notify", StringComparison.OrdinalIgnoreCase))
        {
            await SendExpiryNotificationsAsync();
            await SendTextAsync(chatId, "Уведомления отправлены.");
            return;
        }

        // 3) /ostatki_gbX — старт опитування
        var m = OstatkiCmd.Match(text);
        if (m.Success)
        {
            var num = int.Parse(m.Groups[1].Value);
            var point = $"GB{num}";
            if (num < 1 || num > 10)
            {
                await SendTextAsync(chatId, "Неверная точка. Используйте /ostatki_gb1 ... /ostatki_gb10");
                return;
            }

            // читаємо чергу позицій із “ПОЗИЦИИ”
            var positions = await _tables.ReadPositionsAsync();
            if (positions.Count == 0)
            {
                await SendTextAsync(chatId, "Нет позиций для опроса");
                return;
            }

            _states[chatId] = new UserState
            {
                Point = point,
                Index = 0,
                Positions = positions
            };

            var first = positions[0];
            await SendTextAsync(chatId, $"{first.Name}, срок до {first.Expiry:dd.MM.yyyy}. Введите остаток.");
            return;
        }

        // 4) якщо число — це відповідь на поточну позицію
        if (int.TryParse(text, out var qty))
        {
            if (!_states.TryGetValue(chatId, out var s) || string.IsNullOrWhiteSpace(s.Point) || s.Index >= s.Positions.Count)
            {
                await SendTextAsync(chatId, "Сначала выберите точку: /ostatki_gb1 ... /ostatki_gb10");
                return;
            }

            var item = s.Positions[s.Index];
            if (qty < 0) qty = 0; // нормалізуємо

            // пишемо в «ОТЧЁТЫ» прямо зараз
            await _tables.AppendReportRowAsync(DateTime.Now, chatId, s.Point!, item.Name, item.Expiry, qty);

            // кеш відповіді для підсумкового звіту адміну
            s.Answers.Add((item.Name, item.Expiry, qty));

            s.Index++;
            if (s.Index < s.Positions.Count)
            {
                var next = s.Positions[s.Index];
                await SendTextAsync(chatId, $"{next.Name}, срок до {next.Expiry:dd.MM.yyyy}. Введите остаток.");
            }
            else
            {
                // опитування завершено
                await SendTextAsync(chatId, "Спасибо, ваш ответ записан.");
                await SendAdminReportAsync(chatId, s);
                _states.Remove(chatId);
            }
            return;
        }

        // 5) фолбек
        await SendTextAsync(chatId, "Команда не распознана. Попробуйте /start");
    }

    // === Відправити адмін-зведення + зробити лист “ПОЗИЦИИ” для співробітника ===
    public async Task SendExpiryNotificationsAsync()
    {
        var all = await _tables.GetDataItemsAsync();

        // Адмін: від -inf до 20 днів, left > 0
        var adminItems = all
            .Where(i => i.Days >= double.NegativeInfinity && i.Days <= 20 && i.Left > 0)
            .OrderBy(i => i.Days)
            .ToList();

        // Співробітник: 0..15 днів, left == 0
        var employeeItems = all
            .Where(i => i.Days >= 0 && i.Days <= 15 && i.Left == 0)
            .OrderBy(i => i.Days)
            .ToList();

        // 1) адмін-зведення
        if (adminItems.Count == 0)
        {
            await SendHtmlAsync(_adminChatId, "На данный момент позиций с остатком дней от -20 до 20 нет");
        }
        else
        {
            await SendHtmlAsync(_adminChatId, "<b>Сводка по срокам годности</b>:");

            // 2) групи окремими повідомленнями
            foreach (var groupText in BuildAdminSummaryChunks(adminItems))
            {
                foreach (var chunk in SplitByLength(groupText, 3800))  // твій існуючий helper
                    await SendHtmlAsync(_adminChatId, chunk);
            }
        }

        if (employeeItems.Count == 0)
        {
            await SendTextAsync(_employeeChatId, "На данный момент позиций с остатком дней 0 нет.");
            await _tables.SavePositionsToSheetAsync(_employeeChatId, new List<Position>());
            return;
        }

        var empText = BuildEmployeeNotice(employeeItems);
        foreach (var chunk in SplitByLength(empText, 3800))
            await SendHtmlAsync(_employeeChatId, chunk);

        var toSurvey = employeeItems
            .Select(i => new Position(i.Name, i.Expiry))
            .ToList();

        await _tables.SavePositionsToSheetAsync(_employeeChatId, toSurvey);
    }

    private static IEnumerable<string> BuildAdminSummaryChunks(IEnumerable<DataItem> items)
    {
        var groups = new (Func<DataItem, bool> pred, string emoji, string title)[] {
            (i => i.Days < 0,                 "⚫", "Срок истёк – списать"),
            (i => i.Days == 0,                "🔴", "Критично (0 дней)"),
            (i => i.Days >= 1 && i.Days <= 3, "🟠", "Опасно (1–3 дня)"),
            (i => i.Days >= 4 && i.Days <= 7, "🟡", "Внимание (4–7 дней)"),
            (i => i.Days >= 8 && i.Days <= 14,"🟢", "Осторожно (8–14 дней)"),
            (i => i.Days == 15,               "⚪", "На контроле (15 дней)"),
        };

        foreach (var g in groups)
        {
            var arr = items.Where(g.pred).ToList();
            if (arr.Count == 0) continue;

            var sb = new StringBuilder();
            sb.AppendLine($"{g.emoji} <b>{g.title}</b>:");
            foreach (var i in arr)
                sb.AppendLine($" • {i.Name} — осталось {i.Days} дн., остаток <b>{i.Left}</b>");

            yield return sb.ToString();
        }
    }

    private static string BuildEmployeeNotice(IEnumerable<DataItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>Уведомление по истечению срока (остаток 0)</b>:");
        foreach (var i in items)
            sb.AppendLine($" {GetEmoji(i)} {i.Name} — срок годности до {i.Expiry:dd.MM.yyyy}");
        return sb.ToString();
    }

    private static string GetEmoji(DataItem dataItem)
    {
        return dataItem.Days < 0 ? "⚫" :
               dataItem.Days == 0 ? "🔴" :
               dataItem.Days <= 3 ? "🟠" :
               dataItem.Days <= 7 ? "\U0001f7e1" :
               dataItem.Days <= 14 ? "\U0001f7e2" : "⚪";
    }

    private async Task SendAdminReportAsync(long chatId, UserState s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📦 Новый отчёт:");
        sb.AppendLine($"Точка: {s.Point}");
        sb.AppendLine($"Пользователь: {chatId}");
        foreach (var a in s.Answers)
            sb.AppendLine($" • {a.Name} — до {a.Expiry:dd.MM.yyyy} — остаток: {a.Qty}");

        foreach (var chunk in SplitByLength(sb.ToString(), 3800))
            await SendTextAsync(_adminChatId, chunk);
    }

    private static IEnumerable<string> SplitByLength(string text, int maxLen)
    {
        for (int i = 0; i < text.Length; i += maxLen)
            yield return text.Substring(i, Math.Min(maxLen, text.Length - i));
    }

    public Task SendTextAsync(long chatId, string text)
        => _bot.SendMessage(chatId, text);

    public Task SendHtmlAsync(long chatId, string html)
        => _bot.SendMessage(chatId, html, parseMode: ParseMode.Html);

    public async Task ClearReportsSheet()
    {
        await _tables.ClearReportsSheet();
    }
}
