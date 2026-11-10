// Deterministic random sources for the troop tests.
public sealed class FixedTroopRandom : ITroopRandom
{
    readonly float[] _values;
    int _next;

    public FixedTroopRandom(params float[] values)
    {
        _values = values;
    }

    public float Value()
    {
        float value = _values[_next % _values.Length];
        _next++;
        return value;
    }

    public float Range(float min, float max) => min + Value() * (max - min);
}
