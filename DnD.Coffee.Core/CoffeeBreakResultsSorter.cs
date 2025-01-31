﻿namespace DnD.Coffee.Core;

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
                    var totalX = x.Level1 + x.Level2 + x.Level3 + x.Level4 + x.Level5;
                    var totalY = y.Level1 + y.Level2 + y.Level3 + y.Level4 + y.Level5;
                    comparison = totalY.CompareTo(totalX);
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

                var comparisonResult = resultList[j];
                if (IsBetterOrEqualInAllLevels(comparisonResult, currentResult))
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

        int[] xLevels = [x.Level1, x.Level2, x.Level3, x.Level4, x.Level5];
        int[] yLevels = [y.Level1, y.Level2, y.Level3, y.Level4, y.Level5];

        for (var i = 0; i < xLevels.Length; i++)
        {
            if (xLevels[i] < yLevels[i])
                return false;
            else if (xLevels[i] > yLevels[i])
                strictlyBetterInAtLeastOneLevel = true;
        }

        return strictlyBetterInAtLeastOneLevel;
    }
}