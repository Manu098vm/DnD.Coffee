namespace DnD.Coffee.Core;

public class CoffeeBreakState : ICloneable
{
    public int Hour { get; set; }
    public int WarlockSlots { get; set; }
    public int SorceryPoints { get; set; }
    public bool PactKeeperRodUsed { get; set; }
    public bool BloodWellVialUsed { get; set; }
    public CoffeeBreakResults Spells { get; set; } = new();
    public List<CoffeeBreakActions> Actions { get; set; } = [];

    public object Clone() => CloneState(this);

    protected static CoffeeBreakState CloneState(CoffeeBreakState state)
    {
        return new CoffeeBreakState
        {
            Hour = state.Hour,
            WarlockSlots = state.WarlockSlots,
            SorceryPoints = state.SorceryPoints,
            PactKeeperRodUsed = state.PactKeeperRodUsed,
            BloodWellVialUsed = state.BloodWellVialUsed,
            Spells = state.Spells.Clone(),
            Actions = state.Actions.Select(action => new CoffeeBreakActions
            {
                ActionType = action.ActionType,
                Hour = action.Hour,
                SpellSlotLevel = action.SpellSlotLevel,
                SorceryPointsChanged = action.SorceryPointsChanged,
                WarlockSlotsChanged = action.WarlockSlotsChanged
            }).ToList()
        };
    }
}