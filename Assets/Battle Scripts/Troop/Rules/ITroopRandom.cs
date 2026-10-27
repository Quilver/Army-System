using UnityEngine;

// The random source used by the Troop systems. Every troop draws from its own seeded stream (see SeededTroopRandom),
// so the order in which troops update does not change the outcome of a battle with the same seed.
public interface ITroopRandom
{
    // Uniform in [0, 1).
    float Value();

    // Uniform in [min, max).
    float Range(float min, float max);
}

public static class TroopRandomExtensions
{
    // Uniform point inside the unit circle.
    public static Vector2 InsideUnitCircle(this ITroopRandom random)
    {
        float angle = random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(random.Value());
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
