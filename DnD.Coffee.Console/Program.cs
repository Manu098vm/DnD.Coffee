using DnD.Coffee.Core;
using System.Diagnostics;

namespace Dnd.Coffee.ConsoleApp;

public class Program
{
    public static void Main()
    {
        Console.WriteLine($"DnD.Coffee 0.1 by Manu098vm {Environment.NewLine}");

        int warlockLevel = GetValidatedInput("What's your level in the Warlock class?", 3, 20);
        if (warlockLevel == -1) return;

        int sorcererLevel = GetValidatedInput("What's your level in the Sorcerer class?", 2, 20);
        if (sorcererLevel == -1) return;

        Character character = new()
        {
            WarlockLevel = warlockLevel,
            SorcererLevel = sorcererLevel
        };

        int warlockSlotNumberCurrent = GetValidatedInput($"How many Warlock slots do you have left? (0 to {character.GetWarlockSlotNumberTotal()})", 0, character.GetWarlockSlotNumberTotal());
        if (warlockSlotNumberCurrent == -1) return;

        int sorceryPointsCurrent = GetValidatedInput($"How many Sorcery Points do you have left? (0 to {character.GetSorceryPointsTotal()})", 0, character.GetSorceryPointsTotal());
        if (sorceryPointsCurrent == -1) return;

        bool hasPactKeeperRod = GetYesNoInput("Do you have the Pact Keeper Rod? (Y/n)");
        bool hasBloodWellVial = GetYesNoInput("Do you have the Blood Well Vial? (Y/n)");

        int sleepHoursTotal = GetValidatedInput("How many hours is your character going to rest?", 0, int.MaxValue);
        if (sleepHoursTotal == -1) return;

        int minimumWarlockSlots = GetValidatedInput($"What's the minimum number of Warlock slots you want to have left? (0 to {character.GetWarlockSlotNumberTotal()})", 0, character.GetWarlockSlotNumberTotal());
        if (minimumWarlockSlots == -1) return;

        int minimumSorceryPoints = GetValidatedInput($"What's the minimum number of Sorcery Points you want to have left? (0 to {character.GetSorceryPointsTotal()})", 0, character.GetSorceryPointsTotal());
        if (minimumSorceryPoints == -1) return;

        var timer = new Stopwatch();
        timer.Start();

        var results = Calculator.CalculateSpellSlots(
            character,
            warlockSlotNumberCurrent,
            sorceryPointsCurrent,
            hasPactKeeperRod,
            hasBloodWellVial,
            sleepHoursTotal,
            minimumSorceryPoints,
            minimumWarlockSlots,
            SortingCriteria.Level5Slots,
            SortingCriteria.Level4Slots,
            SortingCriteria.Level3Slots,
            SortingCriteria.TotalSlots,
            SortingCriteria.Level2Slots,
            SortingCriteria.Level1Slots);

        timer.Stop();
        Console.WriteLine($"{Environment.NewLine}Completed. Found {results.Count} possible combinations in {timer.Elapsed}\n");

        try
        {
            Console.WriteLine("How many results do you want to see?");
            if (int.TryParse(Console.ReadLine(), out int resultsToShow))
            {
                Console.WriteLine("");
                for (int i = 0; i < resultsToShow && i < results.Count; i++)
                {
                    Console.WriteLine($"Option {i + 1}:");
                    Console.WriteLine(results.ElementAt(i));
                }
            }
            else
            {
                Console.WriteLine("Invalid number of results.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        WaitForInputToExit();
    }

    private static int GetValidatedInput(string prompt, int min, int max)
    {
        Console.WriteLine(prompt);
        if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
        {
            return value;
        }
        Console.WriteLine($"Input must be between {min} and {max}.");
        WaitForInputToExit();
        return -1;
    }

    private static bool GetYesNoInput(string prompt)
    {
        Console.WriteLine(prompt);
        return Console.ReadLine()?.ToLower() == "y";
    }

    public static void WaitForInputToExit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
