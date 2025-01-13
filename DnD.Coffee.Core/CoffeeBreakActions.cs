namespace DnD.Coffee.Core;

public enum CoffeeBreakActionType
{
    StartRest,
    ConvertWarlockSlotIntoSorceryPoints,
    ConvertSorceryPointsIntoSpellSlot,
    UsePactRod,
    UseBloodVial,
    RestoreWarlockSlots,
    EndOfRestBonusConversion
}


public class CoffeeBreakActions
{
    public CoffeeBreakActionType ActionType { get; set; }
    public int Hour { get; set; }
    public int? SpellSlotLevel { get; set; }
    public int? SorceryPointsChanged { get; set; }
    public int? WarlockSlotsChanged { get; set; }

    public override string ToString()
    {
        return ActionType switch
        {
            CoffeeBreakActionType.StartRest => "Start Rest",
            CoffeeBreakActionType.ConvertWarlockSlotIntoSorceryPoints => $"Hour {Hour}: Converted 1 Warlock slot into {SorceryPointsChanged} Sorcery Point(s)",
            CoffeeBreakActionType.ConvertSorceryPointsIntoSpellSlot => $"Hour {Hour}: Created Level {SpellSlotLevel} Spell Slot using {SorceryPointsChanged} Sorcery Point(s)",
            CoffeeBreakActionType.UsePactRod => $"Hour {Hour}: Used Pact Rod to recover {WarlockSlotsChanged} Warlock Slot(s)",
            CoffeeBreakActionType.UseBloodVial => $"Hour {Hour}: Used Blood Vial to recover {SorceryPointsChanged} Sorcery Point(s)",
            CoffeeBreakActionType.RestoreWarlockSlots => $"Hour {Hour}: Restored Warlock Slots to {WarlockSlotsChanged}",
            CoffeeBreakActionType.EndOfRestBonusConversion => $"End of Rest: Converted {WarlockSlotsChanged} Warlock Slot(s) into {SorceryPointsChanged} Sorcery Point(s)",
            _ => "Unknown Action"
        };
    }
}