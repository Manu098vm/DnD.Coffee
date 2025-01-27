using DnD.Coffee.Core;
using System.Diagnostics;

namespace DnD.Coffee.ConsoleApp;

public class Program
{
    public static void Main()
    {
        // Example input
        int warlockSlotNumberTotal = 2;
        int warlockSlotNumberCurrent = 2;
        int warlockSlotLevel = 2;
        int sorceryPointsTotal = 5;
        int sorceryPointsCurrent = 5;
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

        var filtered = results.OrderByDescending(r => r.Level5)
                              .ThenByDescending(r => r.Level4)
                              .ThenByDescending(r => r.Level3)
                              .ThenByDescending(r => r.Level2)
                              .ThenByDescending(r => r.Level1)
                              .ToHashSet();

        foreach (var (result, i) in filtered.Select((result, i) => (result, i)))
        {
            Console.WriteLine($"Option {i}:{Environment.NewLine}{result}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
