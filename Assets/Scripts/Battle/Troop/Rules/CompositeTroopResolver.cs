using System.Collections.Generic;

// Pure rules for resolving a composite troop's stats before battle (spec §1.2). No Unity types, so the rules can be
// compiled and tested without the engine. All troop stats are integers (CombatStatLimits).

// One part of a composite troop: a character's resolved line before pairing.
public readonly struct TroopStatLine
{
    // Lowest soldier Speed in the part. Becomes the composite's Move.
    public readonly int Speed;
    public readonly int BaseMorale;
    public readonly int Discipline;
    public readonly int SoldierCount;

    public TroopStatLine(int speed, int baseMorale, int discipline, int soldierCount)
    {
        Speed = speed;
        BaseMorale = baseMorale;
        Discipline = discipline;
        SoldierCount = soldierCount;
    }
}

public readonly struct ResolvedTroopStats
{
    public readonly int Move;
    public readonly int BaseMorale;
    public readonly int Discipline;
    public readonly int InitialSoldierCount;

    public ResolvedTroopStats(int move, int baseMorale, int discipline, int initialSoldierCount)
    {
        Move = move;
        BaseMorale = baseMorale;
        Discipline = discipline;
        InitialSoldierCount = initialSoldierCount;
    }
}

public static class CompositeTroopResolver
{
    // Move = min(Speed_i); BaseMorale = max(Morale_i); Discipline = max(Discipline_i); soldiers = sum(Numbers_i).
    // An empty list resolves to zeros.
    public static ResolvedTroopStats Resolve(IReadOnlyList<TroopStatLine> parts)
    {
        if (parts == null || parts.Count == 0) return new ResolvedTroopStats(0, 0, 0, 0);

        int move = parts[0].Speed;
        int morale = parts[0].BaseMorale;
        int discipline = parts[0].Discipline;
        int soldiers = 0;

        for (int i = 0; i < parts.Count; i++)
        {
            TroopStatLine part = parts[i];
            move = System.Math.Min(move, part.Speed);
            morale = System.Math.Max(morale, part.BaseMorale);
            discipline = System.Math.Max(discipline, part.Discipline);
            soldiers += System.Math.Max(0, part.SoldierCount);
        }

        return new ResolvedTroopStats(move, morale, discipline, soldiers);
    }
}
