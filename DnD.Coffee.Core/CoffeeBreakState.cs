namespace DnD.Coffee.Core;

public class CoffeeBreakState
{
    public int Hour;
    public int WarlockSlots;
    public int SorceryPoints;
    public bool PactKeeperRodUsed;
    public bool BloodWellVialUsed;
    public CoffeeBreakResults Spells = null!;
    public List<CoffeeBreakActions> Actions = null!;

    public static CoffeeBreakState CloneState(CoffeeBreakState state)
    {
        return new CoffeeBreakState
        {
            Hour = state.Hour,
            WarlockSlots = state.WarlockSlots,
            SorceryPoints = state.SorceryPoints,
            PactKeeperRodUsed = state.PactKeeperRodUsed,
            BloodWellVialUsed = state.BloodWellVialUsed,
            Spells = new CoffeeBreakResults
            {
                Level1 = state.Spells.Level1,
                Level2 = state.Spells.Level2,
                Level3 = state.Spells.Level3,
                Level4 = state.Spells.Level4,
                Level5 = state.Spells.Level5
            },
            Actions = new List<CoffeeBreakActions>(state.Actions)
        };
    }
}

