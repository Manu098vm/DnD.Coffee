namespace DnD.Coffee.Core;

public class Character(int warlockLevel, int sorcererLevel)
{
    public int WarlockLevel { get; init; } = warlockLevel;
    public int SorcererLevel { get; init; } = sorcererLevel;

    public int GetWarlockSlotNumberTotal() => WarlockLevel switch
    {
        >= 2 and <= 10 => 2,
        >= 11 and <= 16 => 3,
        >= 17 => 4,
        _ => 1
    };

    public int GetWarlockSlotLevel() => WarlockLevel switch
    {
        >= 1 and <= 2 => 1,
        >= 3 and <= 4 => 2,
        >= 5 and <= 6 => 3,
        >= 7 and <= 8 => 4,
        >= 9 and <= 11 => 5,
        >= 12 and <= 14 => 6,
        >= 15 and <= 17 => 7,
        _ => 8,
    };

    public int GetSorceryPointsTotal() => SorcererLevel switch
    {
        > 1 => SorcererLevel,
        _ => 0,
    };
}