using DnD.Coffee.Core;

namespace DnD.Coffee.ConsoleApp;

public class Program
{
    public static void Main()
    {
        // Example input
        int warlockSlotNumberTotal = 2;
        int warlockSlotNumberCurrent = 0;
        int warlockSlotLevel = 2;
        int sorceryPointsTotal = 5;
        int sorceryPointsCurrent = 0;
        bool hasPactKeeperRod = true;
        bool hasBloodWellVial = true;
        int sleepHoursTotal = 4;

        // Configurable minimum resource requirements
        int minimumSorceryPoints = 5; // Minimum Sorcery Points at the end of rest
        int minimumWarlockSlots = 2;  // Minimum Warlock slots at the end of rest

        var results = Calculator.CalculateSpellSlots(
            warlockSlotNumberTotal,
            warlockSlotNumberCurrent,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            hasPactKeeperRod,
            hasBloodWellVial,
            sleepHoursTotal,
            minimumSorceryPoints,
            minimumWarlockSlots);

        Console.WriteLine($"Completed. Found {results.Count} possible combinations:\n");

        foreach (var result in results)
        {
            Console.WriteLine(result);
        }
    }
}
