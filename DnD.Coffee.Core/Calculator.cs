namespace DnD.Coffee.Core;

public static class Calculator
{
    /// <summary>
    /// Calculates all possible combinations of universal spell slots that can be created during rest.
    /// </summary>
    public static HashSet<CoffeeBreakResults> CalculateSpellSlots(
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
            return [];

        // Check if it's possible to create at least one universal spell slot and recover required resources
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
            return [];

        // Initialize the set of results
        var results = new HashSet<CoffeeBreakResults>(new CoffeeSpellsComparer());
        var initialState = new CoffeeBreakState
        {
            Hour = 0,
            WarlockSlots = warlockSlotNumberCurrent,
            SorceryPoints = sorceryPointsCurrent,
            PactKeeperRodUsed = !pactKeeperRod,
            BloodWellVialUsed = !bloodWellVial,
            Spells = new CoffeeBreakResults(),
            Actions = [ new() { ActionType = CoffeeBreakActionType.StartRest } ]
        };

        // Start searching for possible combinations
        SearchCombinations(
            initialState,
            results,
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
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


        // Calculate the maximum number of Sorcery Points that can be generated
        int potentialSorceryPoints = (sleepHours * warlockSlotNumberTotal * warlockSlotLevel)
            + (pactKeeperRod ? warlockSlotLevel * Constants.PactRodSpellSlotsGain : 0)
            + (bloodWellVial ? Math.Min(Constants.BloodVialSorceryPointsGain, sorceryPointsTotal) : 0);

        // Check if it's possible to have at least the minimum Sorcery Points and create at least one universal spell slot
        return (sorceryPointsCurrent + potentialSorceryPoints) >= (minimumSorceryPoints + Constants.Level1Cost);
    }

    // Method to recursively search for all possible combinations
    private static void SearchCombinations(
        CoffeeBreakState state,
        HashSet<CoffeeBreakResults> results,
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int sorceryPointsCurrent,
        int sleepHours,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        // If we've reached the end of the rest period, perform end-of-rest bonus actions if possible
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

        // Simulate all possible action sequences within the current hour
        SimulateActions(
            state,
            results,
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            sleepHours,
            minimumSorceryPoints,
            minimumWarlockSlots);
    }

    // Function to perform bonus actions at the end of rest
    private static void PerformEndOfRestBonusActions(
        CoffeeBreakState state,
        HashSet<CoffeeBreakResults> results,
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        // Ensure Warlock slots are restored at the end of rest
        if (state.WarlockSlots < warlockSlotNumberTotal)
        {
            state.WarlockSlots = warlockSlotNumberTotal;
            state.Actions.Add(new CoffeeBreakActions
            {
                ActionType = CoffeeBreakActionType.RestoreWarlockSlots,
                Hour = state.Hour,
                WarlockSlotsChanged = warlockSlotNumberTotal
            });
        }

        // Try spending the restored Warlock slots down to minimumWarlockSlots
        int maxSpendableWarlockSlots = state.WarlockSlots - minimumWarlockSlots;

        // For each possible number of Warlock slots to spend
        for (int slotsToSpend = maxSpendableWarlockSlots; slotsToSpend >= 0; slotsToSpend--)
        {
            var newState = CoffeeBreakState.CloneState(state);
            if (slotsToSpend > 0)
            {
                newState.WarlockSlots -= slotsToSpend;
                int sorceryPointsGained = slotsToSpend * warlockSlotLevel;
                newState.SorceryPoints += sorceryPointsGained;

                // Cap Sorcery Points at total limit
                if (newState.SorceryPoints > sorceryPointsTotal)
                {
                    sorceryPointsGained -= newState.SorceryPoints - sorceryPointsTotal;
                    newState.SorceryPoints = sorceryPointsTotal;
                }

                // Record the action
                newState.Actions.Add(new CoffeeBreakActions
                {
                    ActionType = CoffeeBreakActionType.EndOfRestBonusConversion,
                    Hour = newState.Hour,
                    SorceryPointsChanged = sorceryPointsGained,
                    WarlockSlotsChanged = -slotsToSpend
                });
            }

            // Attempt to convert Sorcery Points into spell slots
            ConvertSorceryPointsToSpellSlots(newState, minimumSorceryPoints);

            // Check if the state's resources meet the minimum requirements
            if (newState.SorceryPoints >= minimumSorceryPoints && newState.WarlockSlots >= minimumWarlockSlots &&
                (newState.Spells.Level1 > 0 || newState.Spells.Level2 > 0 || newState.Spells.Level3 > 0 ||
                 newState.Spells.Level4 > 0 || newState.Spells.Level5 > 0))
            {
                var resultSpells = newState.Spells;
                resultSpells.RemainingSorceryPoints = newState.SorceryPoints;
                resultSpells.RemainingWarlockSlots = newState.WarlockSlots;
                resultSpells.ActionsTaken = newState.Actions;
                results.Add(resultSpells);
            }
            if (newState.SorceryPoints >= minimumSorceryPoints && newState.WarlockSlots >= minimumWarlockSlots)
            {
                var resultSpells = newState.Spells;
                resultSpells.RemainingSorceryPoints = newState.SorceryPoints;
                resultSpells.RemainingWarlockSlots = newState.WarlockSlots;
                resultSpells.ActionsTaken = newState.Actions;
                results.Add(resultSpells);
            }
        }
    }

    // Converts Sorcery Points into spell slots after rest
    private static void ConvertSorceryPointsToSpellSlots(
        CoffeeBreakState state,
        int minimumSorceryPoints)
    {
        bool conversionMade = true;
        while (conversionMade)
        {
            conversionMade = false;

            // Attempt to create the highest level spell slots possible
            for (int level = 5; level >= 1; level--)
            {
                int cost = Constants.GetSpellSlotCost(level);
                if (state.SorceryPoints - cost >= minimumSorceryPoints)
                {
                    state.SorceryPoints -= cost;

                    // Increase the appropriate level slot count
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

                    // Record the action
                    state.Actions.Add(new CoffeeBreakActions
                    {
                        ActionType = CoffeeBreakActionType.ConvertSorceryPointsIntoSpellSlot,
                        Hour = state.Hour,
                        SpellSlotLevel = level,
                        SorceryPointsChanged = cost
                    });

                    conversionMade = true;
                    break; // Break to always attempt higher-level slots first
                }
            }
        }
    }

    private static void SimulateActions(
        CoffeeBreakState state,
        HashSet<CoffeeBreakResults> results,
        int warlockSlotNumberTotal,
        int warlockSlotLevel,
        int sorceryPointsTotal,
        int sorceryPointsCurrent,
        int sleepHours,
        int minimumSorceryPoints,
        int minimumWarlockSlots)
    {
        // Calculate the maximum resources recoverable in the remaining hours
        int remainingHours = sleepHours - state.Hour;

        // Interrompo la ricorsione se non è possibile creare uno slot incantesimo
        if (!CanGenerateAtLeastOneSlot(warlockSlotNumberTotal, warlockSlotLevel, sorceryPointsTotal, state.SorceryPoints, state.PactKeeperRodUsed, state.BloodWellVialUsed, remainingHours, 0))
            return;

        // If we've already passed the maximum number of hours, perform end-of-rest bonus actions
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

        // 1. Convert Warlock spell slots to Sorcery Points
        if (state.WarlockSlots > 0 && state.SorceryPoints < sorceryPointsTotal)
        {
            var newState = CoffeeBreakState.CloneState(state);
            newState.WarlockSlots -= 1;
            int pointsGained = warlockSlotLevel;
            newState.SorceryPoints += pointsGained;

            if (newState.SorceryPoints > sorceryPointsTotal)
            {
                pointsGained -= newState.SorceryPoints - sorceryPointsTotal;
                newState.SorceryPoints = sorceryPointsTotal;
            }

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
                sorceryPointsCurrent,
                sleepHours,
                minimumSorceryPoints,
                minimumWarlockSlots);
        }

        // 2. Convert Sorcery Points to universal spell slots
        for (int level = 5; level >= 1; level--)
        {
            int cost = Constants.GetSpellSlotCost(level);
            if (state.SorceryPoints - cost >= 0)
            {
                var newState = CoffeeBreakState.CloneState(state);
                newState.SorceryPoints -= cost;

                // Increase the appropriate level slot
                switch (level)
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
                    SpellSlotLevel = level,
                    SorceryPointsChanged = cost
                });

                SimulateActions(
                    newState,
                    results,
                    warlockSlotNumberTotal,
                    warlockSlotLevel,
                    sorceryPointsTotal,
                    sorceryPointsCurrent,
                    sleepHours,
                    minimumSorceryPoints,
                    minimumWarlockSlots);
            }
        }

        // 3. Use Blood Well Vial, if possible
        if (!state.BloodWellVialUsed && state.SorceryPoints < sorceryPointsTotal)
        {
            int pointsToRecover = Math.Min(Constants.BloodVialSorceryPointsGain, sorceryPointsTotal - state.SorceryPoints);
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
                sorceryPointsCurrent,
                sleepHours,
                minimumSorceryPoints,
                minimumWarlockSlots);
        }

        // 4. Use Pact Keeper Rod, if possible
        if (!state.PactKeeperRodUsed && state.WarlockSlots < warlockSlotNumberTotal)
        {
            var newState = CoffeeBreakState.CloneState(state);
            newState.PactKeeperRodUsed = true;
            newState.WarlockSlots += Constants.PactRodSpellSlotsGain;

            if (newState.WarlockSlots > warlockSlotNumberTotal)
                newState.WarlockSlots = warlockSlotNumberTotal;

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
                sorceryPointsCurrent,
                sleepHours,
                minimumSorceryPoints,
                minimumWarlockSlots);
        }

        // Advance to the next hour after actions
        var nextState = CoffeeBreakState.CloneState(state);
        nextState.Hour += 1;

        // Recover Warlock slots at the end of the hour
        if (nextState.WarlockSlots < warlockSlotNumberTotal)
        {
            nextState.WarlockSlots = warlockSlotNumberTotal;
            nextState.Actions.Add(new CoffeeBreakActions
            {
                ActionType = CoffeeBreakActionType.RestoreWarlockSlots,
                Hour = state.Hour,
                WarlockSlotsChanged = nextState.WarlockSlots
            });
        }

        SimulateActions(
            nextState,
            results,
            warlockSlotNumberTotal,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            sleepHours,
            minimumSorceryPoints,
            minimumWarlockSlots);
    }
}
