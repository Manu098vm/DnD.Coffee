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
    public int Hash => GetHashCode();

    public override string ToString()
    {
        var actions = string.Join("\n", ActionsTaken);
        return $"Level 1: {Level1}, Level 2: {Level2}, Level 3: {Level3}, Level 4: {Level4}, Level 5: {Level5}, " +
               $"Remaining Sorcery Points: {RemainingSorceryPoints}, Remaining Warlock Slots: {RemainingWarlockSlots}\n" +
               $"Actions Taken:\n{actions}\n";
    }

    public override int GetHashCode() => 
        HashCode.Combine(Level1, Level2, Level3, Level4, Level5, RemainingSorceryPoints, RemainingWarlockSlots);
}