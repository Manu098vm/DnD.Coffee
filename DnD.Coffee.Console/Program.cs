using DnD.Coffee.Core;
using System.Diagnostics;

namespace Dnd.Coffee.ConsoleApp;

public class Program
{
    public static void Main()
    {
        Console.WriteLine($"DnD.Coffee 0.1 by Manu098vm {Environment.NewLine}");

        int sorcererLevel = 0;
        int warlockLevel = 0;
        int warlockSlotNumberCurrent = 0;
        int sorceryPointsCurrent = 0;
        bool hasPactKeeperRod = false;
        bool hasBloodWellVial = false;
        int sleepHoursTotal = 0;
        int minimumSorceryPoints = 0;
        int minimumWarlockSlots = 0;

        Character character;

        try
        {
            Console.WriteLine("What's your level in the Sorcerer class?");
            sorcererLevel = int.Parse(Console.ReadLine()!);

            if (sorcererLevel < 2 || sorcererLevel > 20)
            {
                Console.WriteLine("Sorcerer level must be between 2 and 20.");
                WaitForInputToExit();
                return;
            }

            Console.WriteLine("What's your level in the Warlock class?");
            warlockLevel = int.Parse(Console.ReadLine()!);

            if (warlockLevel < 3 || warlockLevel > 20)
            {
                Console.WriteLine("Warlock level must be between 3 and 20.");
                WaitForInputToExit();
                return;
            }

            character = new Character(warlockLevel, sorcererLevel);

            Console.WriteLine("How many Warlock slots do you have left?");
            warlockSlotNumberCurrent = int.Parse(Console.ReadLine()!);

            if (warlockSlotNumberCurrent < 0 || warlockSlotNumberCurrent > character.GetWarlockSlotNumberTotal())
            {
                Console.WriteLine($"Warlock slots must be between 0 and {character.GetWarlockSlotNumberTotal()}.");
                WaitForInputToExit();
                return;
            }

            Console.WriteLine("How many Sorcery Points do you have left?");
            sorceryPointsCurrent = int.Parse(Console.ReadLine()!);

            if (sorceryPointsCurrent < 0 || sorceryPointsCurrent > character.GetSorceryPointsTotal())
            {
                Console.WriteLine($"Sorcery Points must be between 0 and {character.GetSorceryPointsTotal()}.");
                WaitForInputToExit();
                return;
            }

            Console.WriteLine("Do you have the Pact Keeper Rod? (Y/n)");
            hasPactKeeperRod = Console.ReadLine()?.ToLower() == "y";

            Console.WriteLine("Do you have the Blood Well Vial? (Y/n)");
            hasBloodWellVial = Console.ReadLine()?.ToLower() == "y";

            Console.WriteLine("How many hours is your character going to rest?");
            sleepHoursTotal = int.Parse(Console.ReadLine()!);

            if (sleepHoursTotal < 0)
            {
                Console.WriteLine("Sleep hours must be greater than 0.");
                WaitForInputToExit();
                return;
            }

            Console.WriteLine("What's the minimum number of Warlock slots you want to have left?");
            minimumWarlockSlots = int.Parse(Console.ReadLine()!);

            if (minimumWarlockSlots < 0 || minimumWarlockSlots > character.GetWarlockSlotNumberTotal())
            {
                Console.WriteLine($"Minimum Warlock slots must be between 0 and {character.GetWarlockSlotNumberTotal()}.");
                WaitForInputToExit();
                return;
            }

            Console.WriteLine("What's the minimum number of Sorcery Points you want to have left?");
            minimumSorceryPoints = int.Parse(Console.ReadLine()!);

            if (minimumSorceryPoints < 0 || minimumSorceryPoints > character.GetSorceryPointsTotal())
            {
                Console.WriteLine($"Minimum Sorcery Points must be between 0 and {character.GetSorceryPointsTotal()}.");
                WaitForInputToExit();
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            WaitForInputToExit();
            return;
        }

        var timer = new Stopwatch();
        timer.Start();

        var results = Calculator.CalculateSpellSlots(
            character.GetWarlockSlotNumberTotal(),
            warlockSlotNumberCurrent,
            character.GetWarlockSlotLevel(),
            character.GetSorceryPointsTotal(),
            sorceryPointsCurrent,
            hasPactKeeperRod,
            hasBloodWellVial,
            sleepHoursTotal,
            minimumSorceryPoints,
            minimumWarlockSlots);

        timer.Stop();
        Console.WriteLine($"{Environment.NewLine}Completed. Found {results.Count} possible combinations in {timer.Elapsed}\n");

        try
        {
            Console.WriteLine("How many results do you want to see?");
            int resultsToShow = int.Parse(Console.ReadLine()!);

            Console.WriteLine("");
            for (int i = 0; i < resultsToShow; i++)
            {
                Console.WriteLine($"Option {i + 1}:");
                Console.WriteLine(results.ElementAt(i));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        WaitForInputToExit();
    }

    public static void WaitForInputToExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
}