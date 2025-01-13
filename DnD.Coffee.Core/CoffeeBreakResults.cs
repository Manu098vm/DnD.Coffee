namespace DnD.Coffee.Core;

public record CoffeeBreakResults
{
    public int Level1 { get; set; }
    public int Level2 { get; set; }
    public int Level3 { get; set; }
    public int Level4 { get; set; }
    public int Level5 { get; set; }
    public int RemainingSorceryPoints { get; set; }
    public int RemainingWarlockSlots { get; set; }
    public List<CoffeeBreakActions> ActionsTaken { get; set; } = null!;

    public override string ToString()
    {
        var actions = string.Join("\n", ActionsTaken);
        return $"Level 1: {Level1}, Level 2: {Level2}, Level 3: {Level3}, Level 4: {Level4}, Level 5: {Level5}, " +
               $"Remaining Sorcery Points: {RemainingSorceryPoints}, Remaining Warlock Slots: {RemainingWarlockSlots}\n" +
               $"Actions Taken:\n{actions}\n";
    }
}

// Custom comparer for CoffeeSpells to avoid duplicates in HashSet
public class CoffeeSpellsComparer : IEqualityComparer<CoffeeBreakResults>
{
    public bool Equals(CoffeeBreakResults? x, CoffeeBreakResults? y)
    {
        // Compare the spells and remaining resources
        return x!.Level1 == y!.Level1 &&
               x.Level2 == y.Level2 &&
               x.Level3 == y.Level3 &&
               x.Level4 == y.Level4 &&
               x.Level5 == y.Level5 &&
               x.RemainingSorceryPoints == y.RemainingSorceryPoints &&
               x.RemainingWarlockSlots == y.RemainingWarlockSlots;
    }

    public int GetHashCode(CoffeeBreakResults obj)
    {
        return HashCode.Combine(
            obj.Level1, obj.Level2, obj.Level3, obj.Level4, obj.Level5,
            obj.RemainingSorceryPoints, obj.RemainingWarlockSlots);
    }
}
