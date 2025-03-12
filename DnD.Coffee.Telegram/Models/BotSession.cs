namespace DnD.Coffee.Telegram;

public class BotSession
{
    public BotCommand Command { get; set; }
    public int Step { get; set; }
    public Dictionary<string, object> TempData { get; set; } = [];
}
