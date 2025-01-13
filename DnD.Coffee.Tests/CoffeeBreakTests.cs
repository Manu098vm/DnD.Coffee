using DnD.Coffee.Core;

namespace DnD.Coffee.Tests;

public class CoffeeBreakTests
{
    [Fact]
    public void CalculateSpellSlots_ShouldReturnEmpty_WhenSleepHoursLessThanOne()
    {
        // Arrange
        int warlockSlotNumberTotal = 2;
        int warlockSlotNumberCurrent = 0;
        int warlockSlotLevel = 2;
        int sorceryPointsTotal = 5;
        int sorceryPointsCurrent = 0;
        bool pactKeeperRod = true;
        bool bloodWellVial = true;
        int sleepHours = 0;
        int minimumSorceryPoints = 5;
        int minimumWarlockSlots = 2;

        // Act
        var result = Calculator.CalculateSpellSlots(
            warlockSlotNumberTotal,
            warlockSlotNumberCurrent,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            pactKeeperRod,
            bloodWellVial,
            sleepHours,
            minimumSorceryPoints,
            minimumWarlockSlots);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateSpellSlots_ShouldReturnValidResults_WhenValidInput()
    {
        // Arrange
        int warlockSlotNumberTotal = 2;
        int warlockSlotNumberCurrent = 0;
        int warlockSlotLevel = 2;
        int sorceryPointsTotal = 5;
        int sorceryPointsCurrent = 0;
        bool pactKeeperRod = true;
        bool bloodWellVial = true;
        int sleepHours = 4;
        int minimumSorceryPoints = 5;
        int minimumWarlockSlots = 2;

        // Act
        var result = Calculator.CalculateSpellSlots(
            warlockSlotNumberTotal,
            warlockSlotNumberCurrent,
            warlockSlotLevel,
            sorceryPointsTotal,
            sorceryPointsCurrent,
            pactKeeperRod,
            bloodWellVial,
            sleepHours,
            minimumSorceryPoints,
            minimumWarlockSlots);

        // Assert
        Assert.NotEmpty(result);
        foreach (var spells in result)
        {
            //Assert.True(result.Count is 39);
            Assert.True(spells.RemainingSorceryPoints >= minimumSorceryPoints);
            Assert.True(spells.RemainingWarlockSlots >= minimumWarlockSlots);
        }
    }
}