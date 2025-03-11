namespace DnD.Coffee.Core;

public class CoffeeBreakResults
{
    public int Level1 { get; set; }
    public int Level2 { get; set; }
    public int Level3 { get; set; }
    public int Level4 { get; set; }
    public int Level5 { get; set; }

    public int TotalSpellSlots => Level1 + Level2 + Level3 + Level4 + Level5;

    public int RemainingSorceryPoints { get; set; }
    public int RemainingWarlockSlots { get; set; }

    public List<CoffeeBreakActions>? ActionsTaken { get; set; }

    public int Hash => GetHashCode();

    public CoffeeBreakResults Clone()
    {
        return new CoffeeBreakResults
        {
            Level1 = Level1,
            Level2 = Level2,
            Level3 = Level3,
            Level4 = Level4,
            Level5 = Level5,
            RemainingSorceryPoints = RemainingSorceryPoints,
            RemainingWarlockSlots = RemainingWarlockSlots,
            ActionsTaken = ActionsTaken?.Select(action => new CoffeeBreakActions
            {
                ActionType = action.ActionType,
                Hour = action.Hour,
                SpellSlotLevel = action.SpellSlotLevel,
                SorceryPointsChanged = action.SorceryPointsChanged,
                WarlockSlotsChanged = action.WarlockSlotsChanged
            }).ToList()
        };
    }

    public override string ToString()
    {
        var actions = ActionsTaken is null ? "" : string.Join($"{Environment.NewLine}", ActionsTaken);
        return $"Level 1: {Level1}, Level 2: {Level2}, Level 3: {Level3}, Level 4: {Level4}, Level 5: {Level5}, " +
               $"Remaining Sorcery Points: {RemainingSorceryPoints}, Remaining Warlock Slots: {RemainingWarlockSlots}\n" +
               $"Actions Taken:{Environment.NewLine}{actions}{Environment.NewLine}";
    }

    public override int GetHashCode() =>
        HashCode.Combine(Level1, Level2, Level3, Level4, Level5, RemainingSorceryPoints, RemainingWarlockSlots);

    public override bool Equals(object? obj)
    {
        if (obj is CoffeeBreakResults other)
        {
            return Level1 == other.Level1 &&
                   Level2 == other.Level2 &&
                   Level3 == other.Level3 &&
                   Level4 == other.Level4 &&
                   Level5 == other.Level5 &&
                   RemainingSorceryPoints == other.RemainingSorceryPoints &&
                   RemainingWarlockSlots == other.RemainingWarlockSlots;
        }
        return false;
    }
}
