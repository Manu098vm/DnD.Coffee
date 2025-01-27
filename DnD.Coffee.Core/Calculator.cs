using System.Collections.Concurrent;

namespace DnD.Coffee.Core;

public static class Calculator
{
    // Cache per la memoization
    private static readonly ConcurrentDictionary<StateKey, bool> StateCache = new();

    /// <summary>
    /// Calcola tutte le possibili combinazioni di slot incantesimo universali che possono essere creati durante il riposo.
    /// </summary>
    public static ConcurrentBag<CoffeeBreakResults> CalculateSpellSlots(
        int warlockSlotNumberTotal,
        int warlockSlotNumberCurrent,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int sorceryPointsCurrent,
        bool pactKeeperRod,
        bool bloodWellVial,
        int sleepHours,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        if (sleepHours < 1)
            return new ConcurrentBag<CoffeeBreakResults>();

        StateCache.Clear();

        // Verifica se è possibile creare almeno uno slot incantesimo
        bool canGenerateSlots = CanGenerateAtLeastOneSlot(
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            pactKeeperRod,
            bloodWellVial,
            sleepHours,
            minimumSorceryPoints);

        if (!canGenerateSlots)
            return new ConcurrentBag<CoffeeBreakResults>();

        // Inizializza i risultati
        var results = new ConcurrentBag<CoffeeBreakResults>();
        var initialState = new CoffeeBreakState
        {
            Hour = 0,
            WarlockSlots = warlockSlotNumberCurrent,
            SorceryPoints = sorceryPointsCurrent,
            PactKeeperRodUsed = !pactKeeperRod,
            BloodWellVialUsed = !bloodWellVial,
            Spells = new CoffeeBreakResults(),
            Actions = new List<CoffeeBreakActions> { new() { ActionType = CoffeeBreakActionType.StartRest } }
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
        int potentialSorceryPoints = (sleepHours * warlockSlotNumberTotal * warlockSlotLevel)
            + (pactKeeperRod ? Constants.PactRodSpellSlotsGain * warlockSlotLevel : 0)
            + (bloodWellVial ? Math.Min(Constants.BloodVialSorceryPointsGain, sorceryPointsTotal - sorceryPointsCurrent) : 0);

        return (sorceryPointsCurrent + potentialSorceryPoints) >= (minimumSorceryPoints + Constants.Level1Cost);
    }

    // Metodo ricorsivo con memoization e potatura avanzata
    private static void SimulateActions(
        CoffeeBreakState state,
        ConcurrentBag<CoffeeBreakResults> results,
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
                var newState = CoffeeBreakState.CloneState(state);
                newState.WarlockSlots -= 1;
                int pointsGained = warlockSlotLevel;
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
        for (int level = 5; level >= 1; level--)
        {
            int cost = Constants.GetSpellSlotCost(level);
            if (state.SorceryPoints - cost >= 0)
            {
                // Cattura le variabili locali
                int capturedLevel = level;
                int capturedCost = cost;

                tasks.Add(Task.Run(() =>
                {
                    var newState = CoffeeBreakState.CloneState(state);
                    newState.SorceryPoints -= capturedCost;

                    newState.Spells = capturedLevel switch
                    {
                        1 => newState.Spells with { Level1 = newState.Spells.Level1 + 1 },
                        2 => newState.Spells with { Level2 = newState.Spells.Level2 + 1 },
                        3 => newState.Spells with { Level3 = newState.Spells.Level3 + 1 },
                        4 => newState.Spells with { Level4 = newState.Spells.Level4 + 1 },
                        5 => newState.Spells with { Level5 = newState.Spells.Level5 + 1 },
                        _ => newState.Spells
                    };

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


        // 3. Usa Blood Well Vial, se possibile
        if (!state.BloodWellVialUsed && state.SorceryPoints < sorceryPointsTotal)
        {
            tasks.Add(Task.Run(() =>
            {
                int pointsToRecover = Math.Min(
                    Constants.BloodVialSorceryPointsGain,
                    sorceryPointsTotal - state.SorceryPoints);
                var newState = CoffeeBreakState.CloneState(state);
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

        // 4. Usa Pact Keeper Rod, se possibile
        if (!state.PactKeeperRodUsed && state.WarlockSlots < warlockSlotNumberTotal)
        {
            tasks.Add(Task.Run(() =>
            {
                var newState = CoffeeBreakState.CloneState(state);
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
        var nextState = CoffeeBreakState.CloneState(state);
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

        // Continua la ricorsione
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

        // Attende il completamento di tutti i task
        Task.WaitAll(tasks.ToArray());
    }

    // Metodo ripristinato e aggiornato
    private static void PerformEndOfRestBonusActions(
        CoffeeBreakState state,
        ConcurrentBag<CoffeeBreakResults> results,
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

        // Tenta di spendere gli slot Warlock ripristinati
        int maxSpendableWarlockSlots = state.WarlockSlots - minimumWarlockSlots;

        for (int slotsToSpend = maxSpendableWarlockSlots; slotsToSpend >= 0; slotsToSpend--)
        {
            var newState = CoffeeBreakState.CloneState(state);
            if (slotsToSpend > 0)
            {
                newState.WarlockSlots -= slotsToSpend;
                int sorceryPointsGained = slotsToSpend * warlockSlotLevel;
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

            //Check if at least one state managed to create at least one spell slot
            if (newState.Spells.Level1 > 0 || newState.Spells.Level2 > 0 || newState.Spells.Level3 > 0 ||
                 newState.Spells.Level4 > 0 || newState.Spells.Level5 > 0)
            {
                var test = true;
            }

                // Verifica se lo stato soddisfa i requisiti minimi
                if (newState.SorceryPoints >= minimumSorceryPoints && newState.WarlockSlots >= minimumWarlockSlots &&
                (newState.Spells.Level1 > 0 || newState.Spells.Level2 > 0 || newState.Spells.Level3 > 0 ||
                 newState.Spells.Level4 > 0 || newState.Spells.Level5 > 0))
            {
                var resultSpells = newState.Spells with
                {
                    RemainingSorceryPoints = newState.SorceryPoints,
                    RemainingWarlockSlots = newState.WarlockSlots,
                    ActionsTaken = newState.Actions
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
        bool conversionMade = true;
        while (conversionMade)
        {
            conversionMade = false;

            for (int level = 5; level >= 1; level--)
            {
                int cost = Constants.GetSpellSlotCost(level);
                if (state.SorceryPoints - cost >= minimumSorceryPoints)
                {
                    state.SorceryPoints -= cost;

                    state.Spells = level switch
                    {
                        1 => state.Spells with { Level1 = state.Spells.Level1 + 1 },
                        2 => state.Spells with { Level2 = state.Spells.Level2 + 1 },
                        3 => state.Spells with { Level3 = state.Spells.Level3 + 1 },
                        4 => state.Spells with { Level4 = state.Spells.Level4 + 1 },
                        5 => state.Spells with { Level5 = state.Spells.Level5 + 1 },
                        _ => state.Spells
                    };

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

// Definizione della chiave di stato per la cache
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

