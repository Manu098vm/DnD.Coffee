namespace DnD.Coffee.Telegram;

public class UserProfile
{
    public long TelegramId { get; set; }
    public List<CharacterProfile> Characters { get; set; } = [];
    public CharacterProfile? ActiveCharacter { get; set; }
}