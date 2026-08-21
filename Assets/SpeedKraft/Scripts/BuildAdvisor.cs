using UnityEngine;
using System.Collections.Generic;

public static class BuildAdvisor
{
    public class Suggestion
    {
        public PartCategory category;
        public CarPart currentPart;
        public CarPart suggestedPart;
        public float currentScore;
        public float projectedScore;
        public float gain;
    }

    // Chained/sequential search: each suggestion is found on top of the
    // build state AFTER applying the previous suggestion, not against the
    // original build independently. This is the fix for two suggestions
    // both claiming to reach the same ceiling on their own, if the first
    // swap alone already maxes the score, the search correctly finds no
    // further gain available and stops instead of recommending a second,
    // redundant change.
    public static List<Suggestion> GetTopSuggestions(CarBuild build, Circuit circuit, CarPart[] allParts, int maxSuggestions = 2)
    {
        var suggestions = new List<Suggestion>();
        if (build == null || circuit == null || allParts == null) return suggestions;

        var alreadySuggestedCategories = new HashSet<PartCategory>();
        float runningScore = PerformanceEngine.Calculate(build, circuit, requireComplete: false).circuitFitScore;

        for (int i = 0; i < maxSuggestions; i++)
        {
            // Ceiling reached, no further suggestion can possibly help.
            if (runningScore >= 99.5f) break;

            PartCategory? bestCategory = null;
            CarPart bestOriginal = null;
            CarPart bestAlt = null;
            float bestScore = runningScore;

            foreach (PartCategory category in System.Enum.GetValues(typeof(PartCategory)))
            {
                if (alreadySuggestedCategories.Contains(category)) continue;

                CarPart original = GetPart(build, category);

                foreach (var candidate in allParts)
                {
                    if (candidate == null || candidate.category != category || candidate == original)
                        continue;

                    SetPart(build, category, candidate);
                    float trialScore = PerformanceEngine.Calculate(build, circuit, requireComplete: false).circuitFitScore;
                    SetPart(build, category, original);

                    if (trialScore > bestScore)
                    {
                        bestScore = trialScore;
                        bestCategory = category;
                        bestOriginal = original;
                        bestAlt = candidate;
                    }
                }
            }

            // No category improves on the current running state, stop.
            if (bestCategory == null) break;

            suggestions.Add(new Suggestion
            {
                category = bestCategory.Value,
                currentPart = bestOriginal,
                suggestedPart = bestAlt,
                currentScore = runningScore,
                projectedScore = bestScore,
                gain = bestScore - runningScore
            });

            // Actually apply this suggestion before searching for the next
            // one, so the next search reflects the improved state.
            SetPart(build, bestCategory.Value, bestAlt);
            alreadySuggestedCategories.Add(bestCategory.Value);
            runningScore = bestScore;
        }

        // Revert all trial changes, this method must not leave the user's
        // actual build modified.
        foreach (var s in suggestions)
            SetPart(build, s.category, s.currentPart);

        return suggestions;
    }

    static CarPart GetPart(CarBuild b, PartCategory c) => c switch
    {
        PartCategory.FrontWing => b.frontWing,
        PartCategory.RearWing => b.rearWing,
        PartCategory.Engine => b.engine,
        PartCategory.Suspension => b.suspension,
        PartCategory.Tyres => b.tyres,
        PartCategory.Brakes => b.brakes,
        PartCategory.Sidepods => b.sidepods,
        PartCategory.Floor => b.floor,
        _ => null
    };

    static void SetPart(CarBuild b, PartCategory c, CarPart part)
    {
        switch (c)
        {
            case PartCategory.FrontWing: b.frontWing = part; break;
            case PartCategory.RearWing: b.rearWing = part; break;
            case PartCategory.Engine: b.engine = part; break;
            case PartCategory.Suspension: b.suspension = part; break;
            case PartCategory.Tyres: b.tyres = part; break;
            case PartCategory.Brakes: b.brakes = part; break;
            case PartCategory.Sidepods: b.sidepods = part; break;
            case PartCategory.Floor: b.floor = part; break;
        }
    }
}