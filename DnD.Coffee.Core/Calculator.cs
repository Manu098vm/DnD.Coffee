using ConcurrentCollections;
using System.Collections.Concurrent;

namespace DnD.Coffee.Core;

public static class Calculator
{
    // Memoization
    private static readonly ConcurrentDictionary<StateKey, bool> StateCache = new();

    public record StateKey(
    int Hour,
    int SorceryPoints,
    int WarlockSlots,
    bool PactKeeperRodUsed,
    bool BloodWellVialUsed,
    int Level1Spells,
    int Level2Spells,
    int Level3Spells,
    int Level4Spells,
    int Level5Spells);

    /// <summary>
    /// Calcola tutte le possibili combinazioni di slot incantesimo universali che possono essere creati durante un riposo.
    /// </summary>
    public static IEnumerable<CoffeeBreakResults> CalculateSpellSlots(
        Character character,
        int warlockSlotNumberCurrent,
        int sorceryPointsCurrent,
        bool pactKeeperRod,
        bool bloodWellVial,
        int sleepHours,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        if (sleepHours < 1)
            return [];

        StateCache.Clear();

        var warlockSlotNumberTotal = character.GetWarlockSlotNumberTotal();
        var warlockSlotLevel = character.GetWarlockSlotLevel();
        var sorceryPointsTotal = character.GetSorceryPointsTotal();

        // Verifica se è possibile creare almeno uno slot incantesimo
        var canGenerateSlots = CanGenerateAtLeastOneSlot(
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            pactKeeperRod,
            bloodWellVial,
            sleepHours,
            minimumSorceryPoints);

        if (!canGenerateSlots)
            return [];

        // Inizializza i risultati
        var results = new ConcurrentHashSet<CoffeeBreakResults>();
        var initialState = new CoffeeBreakState
        {
            Hour = 0,
            WarlockSlots = warlockSlotNumberCurrent,
            SorceryPoints = sorceryPointsCurrent,
            PactKeeperRodUsed = !pactKeeperRod,
            BloodWellVialUsed = !bloodWellVial,
            Spells = new CoffeeBreakResults(),
            Actions = [new() { ActionType = CoffeeBreakActionType.StartRest }]
        };

        // Avvia la ricerca delle combinazioni
        SimulateActions(
            initialState,
            results,
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sleepHours,
            minimumSorceryPoints,
            minimumWarlockSlots);

        return results;
    }

    private static bool CanGenerateAtLeastOneSlot(
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int sorceryPointsCurrent,
        bool pactKeeperRod,
        bool bloodWellVial,
        int sleepHours,
        int minimumSorceryPoints)
    {
        var potentialSorceryPoints = (sleepHours * warlockSlotNumberTotal * warlockSlotLevel)
            + (pactKeeperRod ? Constants.PactRodSpellSlotsGain * warlockSlotLevel : 0)
            + (bloodWellVial ? Math.Min(Constants.BloodVialSorceryPointsGain, sorceryPointsTotal - sorceryPointsCurrent) : 0);

        return (sorceryPointsCurrent + potentialSorceryPoints) >= (minimumSorceryPoints + Constants.GetSpellSlotCost(1));
    }

    // Metodo ricorsivo con memoization e potatura
    private static void SimulateActions(
        CoffeeBreakState state,
        ConcurrentHashSet<CoffeeBreakResults> results,
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int sleepHours,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        // Creazione della chiave per lo stato corrente
        var stateKey = new StateKey(
            state.Hour,
            state.SorceryPoints,
            state.WarlockSlots,
            state.PactKeeperRodUsed,
            state.BloodWellVialUsed,
            state.Spells.Level1,
            state.Spells.Level2,
            state.Spells.Level3,
            state.Spells.Level4,
            state.Spells.Level5);

        // Verifica se lo stato è già stato elaborato
        if (!StateCache.TryAdd(stateKey, true))
            return;

        // Se abbiamo raggiunto la fine del riposo, eseguiamo le azioni bonus
        if (state.Hour >= sleepHours)
        {
            PerformEndOfRestBonusActions(
                state,
                results,
                warlockSlotNumberTotal,
                warlockSlotLevel,
                sorceryPointsTotal,
                minimumSorceryPoints,
                minimumWarlockSlots);
            return;
        }

        var tasks = new List<Task>();

        // 1. Converti slot Warlock in punti stregoneria
        if (state.WarlockSlots > 0 && state.SorceryPoints < sorceryPointsTotal)
        {
            tasks.Add(Task.Run(() =>
            {
                var newState = (CoffeeBreakState)state.Clone();
                newState.WarlockSlots -= 1;
                var pointsGained = newState.SorceryPoints + warlockSlotLevel > sorceryPointsTotal ? sorceryPointsTotal - newState.SorceryPoints : warlockSlotLevel;
                newState.SorceryPoints = Math.Min(newState.SorceryPoints + pointsGained, sorceryPointsTotal);

                newState.Actions.Add(new CoffeeBreakActions
                {
                    ActionType = CoffeeBreakActionType.ConvertWarlockSlotIntoSorceryPoints,
                    Hour = state.Hour,
                    SorceryPointsChanged = pointsGained,
                    WarlockSlotsChanged = -1
                });

                SimulateActions(
                    newState,
                    results,
                    warlockSlotNumberTotal,
                    warlockSlotLevel,
                    sorceryPointsTotal,
                    sleepHours,
                    minimumSorceryPoints,
                    minimumWarlockSlots);
            }));
        }

        // 2. Converti punti stregoneria in slot incantesimo universali
        for (var level = 5; level >= 1; level--)
        {
            int cost = Constants.GetSpellSlotCost(level);
            if (state.SorceryPoints - cost >= 0)
            {
                var capturedLevel = level;
                var capturedCost = cost;

                tasks.Add(Task.Run(() =>
                {
                    var newState = (CoffeeBreakState)state.Clone();
                    newState.SorceryPoints -= capturedCost;
                    newState.Spells = newState.Spells.Clone();
                    switch (capturedLevel)
                    {
                        case 1:
                            newState.Spells.Level1 += 1;
                            break;
                        case 2:
                            newState.Spells.Level2 += 1;
                            break;
                        case 3:
                            newState.Spells.Level3 += 1;
                            break;
                        case 4:
                            newState.Spells.Level4 += 1;
                            break;
                        case 5:
                            newState.Spells.Level5 += 1;
                            break;
                    }

                    newState.Actions.Add(new CoffeeBreakActions
                    {
                        ActionType = CoffeeBreakActionType.ConvertSorceryPointsIntoSpellSlot,
                        Hour = state.Hour,
                        SpellSlotLevel = capturedLevel,
                        SorceryPointsChanged = -capturedCost
                    });

                    SimulateActions(
                        newState,
                        results,
                        warlockSlotNumberTotal,
                        warlockSlotLevel,
                        sorceryPointsTotal,
                        sleepHours,
                        minimumSorceryPoints,
                        minimumWarlockSlots);
                }));
            }
        }

        // 3. Usa la Blood Well Vial, se possibile
        if (!state.BloodWellVialUsed && state.SorceryPoints < sorceryPointsTotal)
        {
            tasks.Add(Task.Run(() =>
            {
                var pointsToRecover = Math.Min(
                    Constants.BloodVialSorceryPointsGain,
                    sorceryPointsTotal - state.SorceryPoints);
                var newState = (CoffeeBreakState)state.Clone();
                newState.BloodWellVialUsed = true;
                newState.SorceryPoints += pointsToRecover;

                newState.Actions.Add(new CoffeeBreakActions
                {
                    ActionType = CoffeeBreakActionType.UseBloodVial,
                    Hour = state.Hour,
                    SorceryPointsChanged = pointsToRecover
                });

                SimulateActions(
                    newState,
                    results,
                    warlockSlotNumberTotal,
                    warlockSlotLevel,
                    sorceryPointsTotal,
                    sleepHours,
                    minimumSorceryPoints,
                    minimumWarlockSlots);
            }));
        }

        // 4. Usa la Pact Keeper Rod, se possibile
        if (!state.PactKeeperRodUsed && state.WarlockSlots < warlockSlotNumberTotal)
        {
            tasks.Add(Task.Run(() =>
            {
                var newState = (CoffeeBreakState)state.Clone();
                newState.PactKeeperRodUsed = true;
                newState.WarlockSlots = Math.Min(
                    newState.WarlockSlots + Constants.PactRodSpellSlotsGain,
                    warlockSlotNumberTotal);

                newState.Actions.Add(new CoffeeBreakActions
                {
                    ActionType = CoffeeBreakActionType.UsePactRod,
                    Hour = state.Hour,
                    WarlockSlotsChanged = Constants.PactRodSpellSlotsGain,
                });

                SimulateActions(
                    newState,
                    results,
                    warlockSlotNumberTotal,
                    warlockSlotLevel,
                    sorceryPointsTotal,
                    sleepHours,
                    minimumSorceryPoints,
                    minimumWarlockSlots);
            }));
        }

        // Avanza all'ora successiva
        var nextState = (CoffeeBreakState)state.Clone();
        nextState.Hour += 1;

        // Ripristina gli slot Warlock alla fine dell'ora
        if (nextState.WarlockSlots < warlockSlotNumberTotal)
        {
            nextState.WarlockSlots = warlockSlotNumberTotal;
            nextState.Actions.Add(new CoffeeBreakActions
            {
                ActionType = CoffeeBreakActionType.RestoreWarlockSlots,
                Hour = nextState.Hour,
                WarlockSlotsChanged = warlockSlotNumberTotal - state.WarlockSlots
            });
        }

        // Reitera
        tasks.Add(Task.Run(() =>
            SimulateActions(
                nextState,
                results,
                warlockSlotNumberTotal,
                warlockSlotLevel,
                sorceryPointsTotal,
                sleepHours,
                minimumSorceryPoints,
                minimumWarlockSlots)));

        Task.WaitAll([.. tasks]);
    }

    // Alla fine del riposo, è ancora possibile eseguire azioni per recuperare Slot Warlock o Punti Stregoneria
    private static void PerformEndOfRestBonusActions(
        CoffeeBreakState state,
        ConcurrentHashSet<CoffeeBreakResults> results,
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        // Ripristina gli slot Warlock alla fine del riposo
        if (state.WarlockSlots < warlockSlotNumberTotal)
        {
            state.WarlockSlots = warlockSlotNumberTotal;
            state.Actions.Add(new CoffeeBreakActions
            {
                ActionType = CoffeeBreakActionType.RestoreWarlockSlots,
                Hour = state.Hour,
                WarlockSlotsChanged = warlockSlotNumberTotal - state.WarlockSlots
            });
        }

        // Tenta di spendere gli slot ripristinati, se rimangono entro il minimo di Punti Stregoneria e Slot da Warlock
        var maxSpendableWarlockSlots = state.WarlockSlots - minimumWarlockSlots;

        for (var slotsToSpend = maxSpendableWarlockSlots; slotsToSpend >= 0; slotsToSpend--)
        {
            var newState = (CoffeeBreakState)state.Clone();
            if (slotsToSpend > 0)
            {
                newState.WarlockSlots -= slotsToSpend;
                var sorceryPointsGained = slotsToSpend * warlockSlotLevel;
                sorceryPointsGained = newState.SorceryPoints + sorceryPointsGained > sorceryPointsTotal ? sorceryPointsTotal - newState.SorceryPoints : sorceryPointsGained;
                newState.SorceryPoints = Math.Min(newState.SorceryPoints + sorceryPointsGained, sorceryPointsTotal);

                newState.Actions.Add(new CoffeeBreakActions
                {
                    ActionType = CoffeeBreakActionType.EndOfRestBonusConversion,
                    Hour = newState.Hour,
                    SorceryPointsChanged = sorceryPointsGained,
                    WarlockSlotsChanged = -slotsToSpend
                });
            }

            // Converte i punti stregoneria in slot incantesimo
            ConvertSorceryPointsToSpellSlots(newState, minimumSorceryPoints);

            // Verifica i requisiti minimi per i Punti Stregoneria e gli Slot da Warlock
            if (newState.SorceryPoints >= minimumSorceryPoints && newState.WarlockSlots >= minimumWarlockSlots &&
                (newState.Spells.Level1 > 0 || newState.Spells.Level2 > 0 || newState.Spells.Level3 > 0 ||
                 newState.Spells.Level4 > 0 || newState.Spells.Level5 > 0))
            {
                var resultSpells = new CoffeeBreakResults
                {
                    Level1 = newState.Spells.Level1,
                    Level2 = newState.Spells.Level2,
                    Level3 = newState.Spells.Level3,
                    Level4 = newState.Spells.Level4,
                    Level5 = newState.Spells.Level5,
                    RemainingSorceryPoints = newState.SorceryPoints,
                    RemainingWarlockSlots = newState.WarlockSlots,
                    ActionsTaken = new List<CoffeeBreakActions>(newState.Actions)
                };
                results.Add(resultSpells);
            }
        }
    }

    // Converte i punti stregoneria in slot incantesimo dopo il riposo
    private static void ConvertSorceryPointsToSpellSlots(
        CoffeeBreakState state,
        int minimumSorceryPoints)
    {
        var conversionMade = true;
        while (conversionMade)
        {
            conversionMade = false;

            for (var level = 5; level >= 1; level--)
            {
                int cost = Constants.GetSpellSlotCost(level);
                if (state.SorceryPoints - cost >= minimumSorceryPoints)
                {
                    state.SorceryPoints -= cost;

                    switch (level)
                    {
                        case 1:
                            state.Spells.Level1 += 1;
                            break;
                        case 2:
                            state.Spells.Level2 += 1;
                            break;
                        case 3:
                            state.Spells.Level3 += 1;
                            break;
                        case 4:
                            state.Spells.Level4 += 1;
                            break;
                        case 5:
                            state.Spells.Level5 += 1;
                            break;
                    }

                    state.Actions.Add(new CoffeeBreakActions
                    {
                        ActionType = CoffeeBreakActionType.ConvertSorceryPointsIntoSpellSlot,
                        Hour = state.Hour,
                        SpellSlotLevel = level,
                        SorceryPointsChanged = -cost
                    });

                    conversionMade = true;
                    break;
                }
            }
        }
    }
}