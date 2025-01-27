namespace DnD.Coffee.Core;

public static class Constants
{
    private static readonly int[] SpellSlotCosts = [2, 3, 5, 6, 7];

    public const int PactRodSpellSlotsGain = 1;
    public const int BloodVialSorceryPointsGain = 5;

    public static int GetSpellSlotCost(int level)
    {
        if (level < 1 || level > SpellSlotCosts.Length)
            throw new ArgumentOutOfRangeException(nameof(level), $"Invalid spell slot level: {level}");

        return SpellSlotCosts[level - 1];
    }
}