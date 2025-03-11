namespace DnD.Coffee.Core;

public enum SortingCriteria
{
    Level5Slots,
    Level4Slots,
    Level3Slots,
    Level2Slots,
    Level1Slots,
    TotalSlots
}

public class CoffeeBreakResultsComparer(IEnumerable<SortingCriteria> criteria) : IComparer<CoffeeBreakResults>
{
    private readonly List<SortingCriteria> _criteria = criteria.ToList();

    public int Compare(CoffeeBreakResults? x, CoffeeBreakResults? y)
    {
        if (x is null || y is null)
            return 0;

        foreach (var criterion in _criteria)
        {
            var comparison = 0;
            switch (criterion)
            {
                case SortingCriteria.Level5Slots:
                    comparison = y.Level5.CompareTo(x.Level5);
                    break;
                case SortingCriteria.Level4Slots:
                    comparison = y.Level4.CompareTo(x.Level4);
                    break;
                case SortingCriteria.Level3Slots:
                    comparison = y.Level3.CompareTo(x.Level3);
                    break;
                case SortingCriteria.Level2Slots:
                    comparison = y.Level2.CompareTo(x.Level2);
                    break;
                case SortingCriteria.Level1Slots:
                    comparison = y.Level1.CompareTo(x.Level1);
                    break;
                case SortingCriteria.TotalSlots:
                    comparison = x.TotalSpellSlots.CompareTo(y.TotalSpellSlots);
                    break;
                default:
                    throw new ArgumentException($"Unknown sorting criteria: {criterion}");
            }

            if (comparison != 0)
                return comparison;
        }

        int xActions = x.ActionsTaken?.Count ?? 0;
        int yActions = y.ActionsTaken?.Count ?? 0;
        return xActions.CompareTo(yActions);
    }
}

public static class CoffeeBreakResultsSorter
{
    public static IEnumerable<CoffeeBreakResults> SortResults(
        this IEnumerable<CoffeeBreakResults> results,
        params SortingCriteria[] criteria)
    {
        var comparer = new CoffeeBreakResultsComparer(criteria);
        return results.OrderBy(r => r, comparer);
    }

    public static IEnumerable<CoffeeBreakResults> FilterOptimalResults(this IEnumerable<CoffeeBreakResults> results)
    {
        var resultList = results.ToList();
        var optimalResults = new List<CoffeeBreakResults>();

        for (var i = 0; i < resultList.Count; i++)
        {
            var currentResult = resultList[i];
            var isOptimal = true;

            for (var j = 0; j < resultList.Count; j++)
            {
                if (i == j)
                    continue;

                var otherResult = resultList[j];

                if (IsBetterOrEqualInAllLevels(otherResult, currentResult))
                {
                    isOptimal = false;
                    break;
                }

                if (AreSimilar(otherResult, currentResult) && ExtendedComparison(otherResult, currentResult) > 0)
                {
                    isOptimal = false;
                    break;
                }
            }

            if (isOptimal)
                optimalResults.Add(currentResult);
        }

        return optimalResults;
    }

    private static bool IsBetterOrEqualInAllLevels(CoffeeBreakResults x, CoffeeBreakResults y)
    {
        bool strictlyBetterInAtLeastOneLevel = false;

        int[] xLevels = { x.Level1, x.Level2, x.Level3, x.Level4, x.Level5 };
        int[] yLevels = { y.Level1, y.Level2, y.Level3, y.Level4, y.Level5 };

        for (var i = 0; i < xLevels.Length; i++)
        {
            if (xLevels[i] < yLevels[i])
                return false;
            else if (xLevels[i] > yLevels[i])
                strictlyBetterInAtLeastOneLevel = true;
        }

        return strictlyBetterInAtLeastOneLevel;
    }

    private static bool AreSimilar(CoffeeBreakResults x, CoffeeBreakResults y)
    {
        if (x.TotalSpellSlots != y.TotalSpellSlots)
            return false;

        int highestLevel = 0;
        for (int level = 5; level >= 1; level--)
        {
            int xValue = GetSlotsForLevel(x, level);
            int yValue = GetSlotsForLevel(y, level);
            if (xValue != 0 || yValue != 0)
            {
                highestLevel = level;
                break;
            }
        }

        if (highestLevel == 0)
            return true;

        return GetSlotsForLevel(x, highestLevel) == GetSlotsForLevel(y, highestLevel);
    }

    private static int ExtendedComparison(CoffeeBreakResults a, CoffeeBreakResults b)
    {
        int comp = a.Level5.CompareTo(b.Level5);
        if (comp != 0)
            return comp;

        comp = a.Level4.CompareTo(b.Level4);
        if (comp != 0)
            return comp;

        comp = a.Level3.CompareTo(b.Level3);
        if (comp != 0)
            return comp;

        comp = a.Level2.CompareTo(b.Level2);
        if (comp != 0)
            return comp;

        return a.Level1.CompareTo(b.Level1);
    }

    private static int GetSlotsForLevel(CoffeeBreakResults result, int level) => level switch
    {
        1 => result.Level1,
        2 => result.Level2,
        3 => result.Level3,
        4 => result.Level4,
        5 => result.Level5,
        _ => 0,
    };
}
