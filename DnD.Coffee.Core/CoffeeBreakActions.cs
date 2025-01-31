﻿namespace DnD.Coffee.Core;

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
            CoffeeBreakActionType.StartRest => $"Hour {Hour}: Start Rest",
            CoffeeBreakActionType.ConvertWarlockSlotIntoSorceryPoints => $"Hour {Hour}: Converted 1 Warlock slot into {SorceryPointsChanged} Sorcery Points",
            CoffeeBreakActionType.ConvertSorceryPointsIntoSpellSlot => $"Hour {Hour}: Created Level {SpellSlotLevel} Spell Slot using {Math.Abs(SorceryPointsChanged ?? 0)} Sorcery Points",
            CoffeeBreakActionType.UsePactRod => $"Hour {Hour}: Used Pact Rod to recover {WarlockSlotsChanged} Warlock Slot",
            CoffeeBreakActionType.UseBloodVial => $"Hour {Hour}: Used Blood Vial to recover {SorceryPointsChanged} Sorcery Points",
            CoffeeBreakActionType.RestoreWarlockSlots => $"Hour {Hour}: Restored Warlock Slots to {WarlockSlotsChanged}",
            CoffeeBreakActionType.EndOfRestBonusConversion => $"Hour {Hour}: Converted {Math.Abs(WarlockSlotsChanged ?? 0)} Warlock Slot(s) into {SorceryPointsChanged} Sorcery Points",
            _ => "Unknown Action"
        };
    }
}