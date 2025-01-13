namespace DnD.Coffee.Core;

public static class Constants
{
    public const int Level1Cost = 2;
    public const int Level2Cost = 3;
    public const int Level3Cost = 5;
    public const int Level4Cost = 6;
    public const int Level5Cost = 7;
    public const int PactRodSpellSlotsGain = 1;
    public const int BloodVialSorceryPointsGain = 5;

    // Gets the Sorcery Point cost for a spell slot of a given level
    public static int GetSpellSlotCost(int level)
    {
        return level switch
        {
            1 => Level1Cost,
            2 => Level2Cost,
            3 => Level3Cost,
            4 => Level4Cost,
            5 => Level5Cost,
            _ => int.MaxValue
        };
    }
}
