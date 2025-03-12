using DnD.Coffee.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace DnD.Coffee.Telegram;

public class Program
{
    private static readonly ConcurrentDictionary<long, BotSession> Sessions = new();
    private static ConcurrentDictionary<long, UserProfile> Users = null!;

    public static async Task Main()
    {
        string token;
        var tokenTextPath = Path.Combine(Environment.CurrentDirectory, "token.config");
        Users = await Utils.LoadUsersAsync();

        while (!File.Exists(tokenTextPath))
        {
            Console.WriteLine("Please input a valid Telegram Bot Token:");
            token = Console.ReadLine()!;
            await File.WriteAllTextAsync(tokenTextPath, token);
        }

        token = await File.ReadAllTextAsync(tokenTextPath);
        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        botClient.StartReceiving(
            updateHandler: async (client, update, ct) => await HandleUpdateAsync(client, update, ct),
            errorHandler: Utils.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMe();
        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await ProcessCallback(botClient, update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Type == UpdateType.Message && update.Message!.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text.Trim();

            if (!Users.ContainsKey(chatId))
            {
                Users[chatId] = new UserProfile { TelegramId = chatId };
                await Utils.SaveUsersAsync(Users);
                if (!messageText.Equals("/newpg"))
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Welcome! Please use /newpg to create a new character.",
                        cancellationToken: cancellationToken
                    );
                }
            }

            if (messageText.StartsWith('/'))
            {
                messageText = messageText.ToLower();
                Sessions.TryRemove(chatId, out _);

                switch (messageText)
                {
                    case "/newpg":
                        await StartNewPG(botClient, chatId, cancellationToken);
                        break;
                    case "/selectpg":
                        await ProcessSelectPG(botClient, chatId, cancellationToken);
                        break;
                    case "/editpg":
                        await ProcessEditPG(botClient, chatId, cancellationToken);
                        break;
                    case "/viewpg":
                        await ProcessViewPG(botClient, chatId, cancellationToken);
                        break;
                    case "/removepg":
                        await ProcessRemovePG(botClient, chatId, cancellationToken);
                        break;
                    case "/rest":
                        await ProcessRest(botClient, chatId, cancellationToken);
                        break;
                    default:
                        await botClient.SendMessage(chatId, "Command not recognized.", cancellationToken: cancellationToken);
                        break;
                }
            }
            else
            {
                if (Sessions.TryGetValue(chatId, out BotSession session))
                {
                    switch (session.Command)
                    {
                        case BotCommand.NewPG:
                            await ProcessNewPGAnswer(botClient, chatId, messageText, cancellationToken);
                            break;
                        case BotCommand.Rest:
                            await ProcessRestAnswer(botClient, chatId, messageText, cancellationToken);
                            break;
                        default:
                            await botClient.SendMessage(chatId, "Unexpected response.", cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
        }
    }

    #region Command Handlers
    private static async Task StartNewPG(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        Sessions[chatId] = new BotSession { Command = BotCommand.NewPG, Step = 0, TempData = [] };
        await botClient.SendMessage(
            chatId: chatId,
            text: "Enter your new character's name:",
            cancellationToken: cancellationToken
        );
    }

    private static async Task ProcessNewPGAnswer(ITelegramBotClient botClient, long chatId, string answer, CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(chatId, out BotSession session) || session.Command != BotCommand.NewPG)
            return;

        switch (session.Step)
        {
            case 0:
                var name = answer.Trim();

                if (Users[chatId].Characters.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    await botClient.SendMessage(chatId, "Character already exists. Please use another name.", cancellationToken: cancellationToken);
                    Sessions.TryRemove(chatId, out _);
                    return;
                }

                if (name.Length > 30)
                {
                    await botClient.SendMessage(chatId, "Character name is too long. Please use a name with 30 characters or less.", cancellationToken: cancellationToken);
                    Sessions.TryRemove(chatId, out _);
                    return;
                }

                session.TempData["Name"] = name;
                session.Step++;
                await botClient.SendMessage(chatId, "Select Warlock level:", replyMarkup: Utils.CreateNumberKeyboard(1, 20, "warlock", "newpg"), cancellationToken: cancellationToken);
                //Handle sorcerer in callback
                break;
            default:
                await botClient.SendMessage(chatId, "Unexpected answer.", cancellationToken: cancellationToken);
                break;
        }
    }

    private static async Task ProcessSelectPG(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        if (Users[chatId].Characters.Count == 0)
        {
            await botClient.SendMessage(chatId, "You have no characters. Please create one with /newpg.", cancellationToken: cancellationToken);
            return;
        }

        var buttons = Users[chatId].Characters.Select(c =>
            InlineKeyboardButton.WithCallbackData(c.Name, $"selectpg|{c.Name}")
        ).ToArray();

        var keyboard = new InlineKeyboardMarkup([buttons]);

        await botClient.SendMessage(
            chatId: chatId,
            text: "Select a character to use:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    private static async Task ProcessEditPG(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var user = Users[chatId];
        if (user.ActiveCharacter == null)
        {
            await botClient.SendMessage(chatId, "No active character. Use /selectpg or /newpg first.", cancellationToken: cancellationToken);
            return;
        }

        Sessions[chatId] = new BotSession { Command = BotCommand.EditPG, Step = 0, TempData = [] };

        await botClient.SendMessage(
            chatId: chatId,
            text: $"Editing character {user.ActiveCharacter.Name}.{Environment.NewLine}Select new Warlock level:",
            replyMarkup: Utils.CreateNumberKeyboard(1, 20, "warlock", "editpg"),
            cancellationToken: cancellationToken
        );
    }

    private static async Task ProcessViewPG(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var user = Users[chatId];
        if (user.ActiveCharacter == null)
        {
            await botClient.SendMessage(chatId, "No active character. Use /selectpg or /newpg first.", cancellationToken: cancellationToken);
            return;
        }

        var activeCharacter = user.ActiveCharacter;
        var message = $"- Name: {activeCharacter.Name}{Environment.NewLine}" +
            $"- Warlock Level: {activeCharacter.WarlockLevel}{Environment.NewLine}" +
            $"- Sorcerer Level: {activeCharacter.SorcererLevel}{Environment.NewLine}" +
            $"- Rod of the Pact Keeper: {(activeCharacter.HasRod ? "Yes" : "No")}{Environment.NewLine}" +
            $"- Bloodwell Vial: {(activeCharacter.HasVial ? "Yes" : "No")}";

        await botClient.SendMessage(chatId, message, cancellationToken: cancellationToken);
    }

    private static async Task ProcessRemovePG(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var user = Users[chatId];
        if (user.ActiveCharacter == null)
        {
            await botClient.SendMessage(chatId, "No active character to remove. Use /selectpg or /newpg first.", cancellationToken: cancellationToken);
            return;
        }

        var activeCharacter = user.ActiveCharacter;
        user.Characters.Remove(activeCharacter);
        user.ActiveCharacter = null;
        await Utils.SaveUsersAsync(Users);

        await botClient.SendMessage(chatId, $"Character {activeCharacter.Name} removed. Please select a new character with /selectpg or create a new one with /newpg.", cancellationToken: cancellationToken);
    }

    private static async Task ProcessRest(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var user = Users[chatId];
        if (user.ActiveCharacter == null)
        {
            await botClient.SendMessage(chatId, "No active character. Use /selectpg or /newpg first.", cancellationToken: cancellationToken);
            return;
        }

        Sessions[chatId] = new BotSession { Command = BotCommand.Rest, Step = 0, TempData = [] };

        var active = user.ActiveCharacter;
        Character character = new() { WarlockLevel = active.WarlockLevel, SorcererLevel = active.SorcererLevel };

        Sessions[chatId].TempData["Character"] = character;
        Sessions[chatId].TempData["ActiveCharacter"] = active;

        await botClient.SendMessage(
            chatId: chatId,
            text: $"How many Warlock Spell Slots remain? (0 to {character.GetWarlockSlotNumberTotal()})",
            replyMarkup: Utils.CreateNumberKeyboard(0, character.GetWarlockSlotNumberTotal(), "warlockRemaining", "rest"),
            cancellationToken: cancellationToken);

        Sessions[chatId].Step++;
    }

    private static async Task ProcessRestAnswer(ITelegramBotClient botClient, long chatId, string answer, CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(chatId, out BotSession session) || session.Command != BotCommand.Rest)
            return;

        var character = (Character)session.TempData["Character"];
        switch (session.Step)
        {
            case 4:
                if (int.TryParse(answer, out int hours) && hours > 0)
                {
                    session.TempData["RestHours"] = hours;
                    int warlockSlotsRemaining = Convert.ToInt32(session.TempData["WarlockRemaining"]);
                    int sorceryPointsRemaining = Convert.ToInt32(session.TempData["SorceryRemaining"]);
                    int warlockMin = Convert.ToInt32(session.TempData["WarlockMin"]);
                    int sorceryMin = Convert.ToInt32(session.TempData["SorceryMin"]);
                    bool rodAvailable = session.TempData.ContainsKey("RodAvailable") && Convert.ToBoolean(session.TempData["RodAvailable"]);
                    bool vialAvailable = session.TempData.ContainsKey("VialAvailable") && Convert.ToBoolean(session.TempData["VialAvailable"]);

                    await botClient.SendMessage(chatId, "Starting the calculations, please wait...", cancellationToken: cancellationToken);
                    var timer = new Stopwatch();
                    timer.Start();

                    var results = Calculator.CalculateSpellSlots(
                        character,
                        warlockSlotsRemaining,
                        sorceryPointsRemaining,
                        rodAvailable,
                        vialAvailable,
                        hours,
                        sorceryMin,
                        warlockMin
                    );

                    timer.Stop();
                    if (!results.Any())
                    {
                        await botClient.SendMessage(chatId, "No possible combinations were found.", cancellationToken: cancellationToken);
                        Sessions.TryRemove(chatId, out _);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, $"Calculation completed. Found {results.Count()} combinations in {timer.Elapsed}.{Environment.NewLine}Refining optimal results...", cancellationToken: cancellationToken);
                        timer.Restart();
                        var sortedResults = results.FilterOptimalResults().SortResults(
                            SortingCriteria.Level5Slots,
                            SortingCriteria.Level4Slots,
                            SortingCriteria.Level3Slots,
                            SortingCriteria.TotalSlots,
                            SortingCriteria.Level2Slots,
                            SortingCriteria.Level1Slots
                        );
                        session.TempData["Results"] = sortedResults;
                        timer.Stop();
                        await botClient.SendMessage(chatId, $"Optimized to {sortedResults.Count()} results in {timer.Elapsed}.  How many would you like to view? Please type a number (max {sortedResults.Count()}).", cancellationToken: cancellationToken);
                        session.Step++;
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, "Please provide a valid positive number for rest hours.", cancellationToken: cancellationToken);
                }
                break;
            case 5:
                if (int.TryParse(answer, out int numToShow) && numToShow > 0)
                {
                    var results = ((IEnumerable<CoffeeBreakResults>)session.TempData["Results"]).ToList();
                    numToShow = Math.Min(numToShow, results.Count);
                    for (int i = 0; i < numToShow; i++)
                    {
                        await botClient.SendMessage(chatId, $"Option {i + 1}:\n{results[i]}", cancellationToken: cancellationToken);
                    }
                    Sessions.TryRemove(chatId, out _);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Please provide a valid positive number.", cancellationToken: cancellationToken);
                }
                break;
            default:
                await botClient.SendMessage(chatId, "Unexpected input.", cancellationToken: cancellationToken);
                break;
        }
    }
    #endregion

    #region Callback Handlers
    private static async Task ProcessCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data;

        if (string.IsNullOrEmpty(data))
            return;
        var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return;

        switch (parts[0])
        {
            case "newpg":
                await ProcessNewPGCallback(botClient, chatId, parts, cancellationToken);
                break;
            case "selectpg":
                // parts[1] is character name
                var selectedName = parts[1];
                Users[chatId].ActiveCharacter = Users[chatId].Characters.FirstOrDefault(c => c.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
                await Utils.SaveUsersAsync(Users);
                await botClient.SendMessage(chatId, $"Active character set to {selectedName}.", cancellationToken: cancellationToken);
                break;
            case "editpg":
                await ProcessEditPGCallback(botClient, chatId, parts, cancellationToken);
                break;
            case "rest":
                await ProcessRestCallback(botClient, chatId, parts, cancellationToken);
                break;
        }

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    private static async Task ProcessNewPGCallback(ITelegramBotClient botClient, long chatId, string[] parts, CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(chatId, out BotSession session) || session.Command != BotCommand.NewPG)
            return;
        if (parts.Length < 3)
            return;
        string param = parts[1];
        string value = parts[2];

        if (param.Equals("warlock", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["WarlockLevel"] = int.Parse(value);
            session.Step++; // next step: sorcerer level
            await botClient.SendMessage(
                chatId,
                "Select Sorcerer level:",
                replyMarkup: Utils.CreateNumberKeyboard(1, 20, "sorcerer", "newpg"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("sorcerer", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["SorcererLevel"] = int.Parse(value);
            session.Step++; // next step: Rod selection
            await botClient.SendMessage(
                chatId,
                "Do you possess the Rod of the Pact Keeper?",
                replyMarkup: Utils.CreateYesNoKeyboard("newpg|rod"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("rod", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["HasRod"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            session.Step++; // next step: Bloodwell Vial selection
            await botClient.SendMessage(
                chatId,
                "Do you possess the Bloodwell Vial?",
                replyMarkup: Utils.CreateYesNoKeyboard("newpg|vial"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("vial", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["HasVial"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);

            int warlock = Convert.ToInt32(session.TempData["WarlockLevel"]);
            int sorcerer = Convert.ToInt32(session.TempData["SorcererLevel"]);
            if (warlock < 3 || sorcerer < 2)
            {
                await botClient.SendMessage(
                    chatId,
                    "Invalid character: Warlock level must be at least 3 and Sorcerer level at least 2. Character not registered.",
                    cancellationToken: cancellationToken);
                Sessions.TryRemove(chatId, out _);
                return;
            }

            var newCharacter = new CharacterProfile
            {
                Name = session.TempData["Name"].ToString()!,
                WarlockLevel = warlock,
                SorcererLevel = sorcerer,
                HasRod = Convert.ToBoolean(session.TempData["HasRod"]),
                HasVial = Convert.ToBoolean(session.TempData["HasVial"])
            };
            Users[chatId].Characters.Add(newCharacter);
            Users[chatId].ActiveCharacter = newCharacter;
            await Utils.SaveUsersAsync(Users);

            await botClient.SendMessage(
                chatId,
                $"Character {newCharacter.Name} registered and set as active.",
                cancellationToken: cancellationToken);

            Sessions.TryRemove(chatId, out _);
        }
    }

    private static async Task ProcessEditPGCallback(ITelegramBotClient botClient, long chatId, string[] parts, CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(chatId, out BotSession session) || session.Command != BotCommand.EditPG)
            return;
        if (parts.Length < 3)
            return;
        string param = parts[1];
        string value = parts[2];

        if (param.Equals("warlock", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["WarlockLevel"] = int.Parse(value);
            session.Step++;
            await botClient.SendMessage(
                chatId,
                "Select new Sorcerer level:",
                replyMarkup: Utils.CreateNumberKeyboard(1, 20, "sorcerer", "editpg"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("sorcerer", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["SorcererLevel"] = int.Parse(value);
            session.Step++;
            await botClient.SendMessage(
                chatId,
                "Do you possess the Rod of the Pact Keeper?",
                replyMarkup: Utils.CreateYesNoKeyboard("editpg|rod"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("rod", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["HasRod"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            session.Step++;
            await botClient.SendMessage(
                chatId,
                "Do you possess the Bloodwell Vial?",
                replyMarkup: Utils.CreateYesNoKeyboard("editpg|vial"),
                cancellationToken: cancellationToken);
        }
        else if (param.Equals("vial", StringComparison.OrdinalIgnoreCase))
        {
            session.TempData["HasVial"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);

            int warlock = Convert.ToInt32(session.TempData["WarlockLevel"]);
            int sorcerer = Convert.ToInt32(session.TempData["SorcererLevel"]);
            if (warlock < 3 || sorcerer < 2)
            {
                await botClient.SendMessage(
                    chatId,
                    "Invalid character: Warlock level must be at least 3 and Sorcerer level at least 2. Changes not saved.",
                    cancellationToken: cancellationToken);
                Sessions.TryRemove(chatId, out _);
                return;
            }

            var active = Users[chatId].ActiveCharacter!;
            active.WarlockLevel = warlock;
            active.SorcererLevel = sorcerer;
            active.HasRod = Convert.ToBoolean(session.TempData["HasRod"]);
            active.HasVial = Convert.ToBoolean(session.TempData["HasVial"]);
            await Utils.SaveUsersAsync(Users);

            await botClient.SendMessage(
                chatId,
                $"Active character {active.Name} updated.",
                cancellationToken: cancellationToken);

            Sessions.TryRemove(chatId, out _);
        }
    }

    private static async Task ProcessRestCallback(ITelegramBotClient botClient, long chatId, string[] parts, CancellationToken cancellationToken)
    {
        if (!Sessions.TryGetValue(chatId, out BotSession session) || session.Command != BotCommand.Rest)
            return;
        if (parts.Length < 3)
            return;
        string param = parts[1];
        string value = parts[2];

        switch (param)
        {
            case "warlockRemaining":
                session.TempData["WarlockRemaining"] = int.Parse(value);
                var character = (Character)session.TempData["Character"];
                await botClient.SendMessage(
                    chatId,
                    $"How many Sorcery Points remain? (0 to {character.GetSorceryPointsTotal()})",
                    replyMarkup: Utils.CreateNumberKeyboard(0, character.GetSorceryPointsTotal(), "sorceryRemaining", "rest"),
                    cancellationToken: cancellationToken);
                session.Step++;
                break;
            case "sorceryRemaining":
                session.TempData["SorceryRemaining"] = int.Parse(value);
                character = (Character)session.TempData["Character"];
                await botClient.SendMessage(
                    chatId,
                    $"Select minimum Warlock Spell Slots to keep (0 to {character.GetWarlockSlotNumberTotal()})",
                    replyMarkup: Utils.CreateNumberKeyboard(0, character.GetWarlockSlotNumberTotal(), "warlockMin", "rest"),
                    cancellationToken: cancellationToken);
                session.Step++;
                break;
            case "warlockMin":
                session.TempData["WarlockMin"] = int.Parse(value);
                character = (Character)session.TempData["Character"];
                await botClient.SendMessage(
                    chatId,
                    $"Select minimum Sorcery Points to keep (0 to {character.GetSorceryPointsTotal()})",
                    replyMarkup: Utils.CreateNumberKeyboard(0, character.GetSorceryPointsTotal(), "sorceryMin", "rest"),
                    cancellationToken: cancellationToken);
                session.Step++;
                break;
            case "sorceryMin":
                session.TempData["SorceryMin"] = int.Parse(value);
                var active = (CharacterProfile)session.TempData["ActiveCharacter"];
                if (active.HasRod)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Is the Rod of the Pact Keeper available?",
                        replyMarkup: Utils.CreateYesNoKeyboard("rest|rod"),
                        cancellationToken: cancellationToken);
                    session.Step++;
                }
                else if (active.HasVial)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Is the Bloodwell Vial available?",
                        replyMarkup: Utils.CreateYesNoKeyboard("rest|vial"),
                        cancellationToken: cancellationToken);
                    session.Step++;
                }
                else
                {
                    await botClient.SendMessage(
                        chatId,
                        "Enter the number of hours of rest:",
                        cancellationToken: cancellationToken);
                    session.Step = 4;
                }
                break;
            case "rod":
                session.TempData["RodAvailable"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                active = (CharacterProfile)session.TempData["ActiveCharacter"];
                if (active.HasVial)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Is the Bloodwell Vial available?",
                        replyMarkup: Utils.CreateYesNoKeyboard("rest|vial"),
                        cancellationToken: cancellationToken);
                    session.Step++;
                }
                else
                {
                    await botClient.SendMessage(chatId, "Enter the number of hours of rest:", cancellationToken: cancellationToken);
                    session.Step = 4;
                }
                break;
            case "vial":
                session.TempData["VialAvailable"] = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                await botClient.SendMessage(chatId, "Enter the number of hours of rest:", cancellationToken: cancellationToken);
                session.Step = 4;
                break;
        }
    }
    #endregion
}