using System.Collections.Concurrent;
using System.Text.Json;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace DnD.Coffee.Telegram;

public static class Utils
{
    private static readonly string UsersJsonPath = Path.Combine(Environment.CurrentDirectory, "users.json");

    public static InlineKeyboardMarkup CreateNumberKeyboard(int start, int end, string param, string callbackPrefix)
    {
        var rows = new List<List<InlineKeyboardButton>>();
        var row = new List<InlineKeyboardButton>();
        int count = 0;

        for (int i = start; i <= end; i++)
        {
            row.Add(InlineKeyboardButton.WithCallbackData(i.ToString(), $"{callbackPrefix}|{param}|{i}"));
            count++;

            if (count >= 5)
            {
                rows.Add(row);
                row = [];
                count = 0;
            }
        }

        if (row.Count != 0)
            rows.Add(row);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup CreateYesNoKeyboard(string prefix)
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Yes", $"{prefix}|Yes"),
            InlineKeyboardButton.WithCallbackData("No", $"{prefix}|No")
        });
    }

    public static async Task<ConcurrentDictionary<long, UserProfile>> LoadUsersAsync()
    {
        if (!File.Exists(UsersJsonPath))
            return new ConcurrentDictionary<long, UserProfile>();

        var json = await File.ReadAllTextAsync(UsersJsonPath);
        var dict = JsonSerializer.Deserialize<Dictionary<long, UserProfile>>(json) ?? [];
        return new ConcurrentDictionary<long, UserProfile>(dict);
    }

    public static async Task SaveUsersAsync(ConcurrentDictionary<long, UserProfile> users)
    {
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(UsersJsonPath, json);
    }

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
