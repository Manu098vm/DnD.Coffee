using DnD.Coffee.Core;

namespace DnD.Coffee.Tests;

public class CoffeeBreakTests
{
    [Fact]
    public void CalculateSpellSlots_ShouldReturnEmpty_WhenSleepHoursLessThanOne()
    {
        // Arrange
        var character = new Character
        {
            WarlockLevel = 3,
            SorcererLevel = 5
        };

        var warlockSlotNumberCurrent = 0;
        var sorceryPointsCurrent = 0;
        var pactKeeperRod = true;
        var bloodWellVial = true;
        var sleepHours = 0;
        var minimumSorceryPoints = 0;
        var minimumWarlockSlots = 0;

        // Act
        var result = Calculator.CalculateSpellSlots(
            character,
            warlockSlotNumberCurrent,
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
        var character = new Character
        {
            WarlockLevel = 3,
            SorcererLevel = 5
        };

        var warlockSlotNumberCurrent = 0;
        var sorceryPointsCurrent = 0;
        var pactKeeperRod = true;
        var bloodWellVial = true;
        var sleepHours = 4;
        var minimumSorceryPoints = 5;
        var minimumWarlockSlots = 2;

        // Act
        var result = Calculator.CalculateSpellSlots(
            character,
            warlockSlotNumberCurrent,
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
            Assert.True(spells.RemainingSorceryPoints >= minimumSorceryPoints);
            Assert.True(spells.RemainingWarlockSlots >= minimumWarlockSlots);
        }
    }
}