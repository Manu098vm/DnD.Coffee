using DnD.Coffee.Core;
using System.Diagnostics;

namespace DnD.Coffee.ConsoleApp;

public class Program
{
    public static void Main()
    {
        // Example input
        int warlockSlotNumberTotal = 2;
        int warlockSlotNumberCurrent = 1;
        int warlockSlotLevel = 2;
        int sorceryPointsTotal = 5;
        int sorceryPointsCurrent = 0;
        bool hasPactKeeperRod = true;
        bool hasBloodWellVial = true;
        int sleepHoursTotal = 4;

        // Configurable minimum resource requirements
        int minimumSorceryPoints = 5; // Minimum Sorcery Points at the end of rest
        int minimumWarlockSlots = 2;  // Minimum Warlock slots at the end of rest

        var timer = new Stopwatch();
        timer.Start();

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

        timer.Stop();
        Console.WriteLine($"Completed. Found {results.Count} possible combinations in {timer.Elapsed}\n");

        foreach (var result in results)
        {
            Console.WriteLine(result);
        }
        Console.ReadKey();
    }
}
