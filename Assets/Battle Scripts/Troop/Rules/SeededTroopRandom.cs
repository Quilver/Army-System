using System;

// Deterministic ITroopRandom backed by System.Random. A battle has one seed (TroopCombatWorld.Seed). Each troop derives
// its own stream from that seed and a stable stream id, so draws for one troop never depend on another troop's updates.
public sealed class SeededTroopRandom : ITroopRandom
{
    readonly Random _random;

    public SeededTroopRandom(int seed)
    {
        _random = new Random(seed);
    }

    public float Value() => (float)_random.NextDouble();

    public float Range(float min, float max) => min + Value() * (max - min);

    // Mixes a battle seed and a stream id into a new seed (SplitMix64 finaliser).
    public static int DeriveSeed(int battleSeed, int streamId)
    {
        unchecked
        {
            ulong z = ((ulong)(uint)battleSeed << 32) | (uint)streamId;
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (int)(uint)(z ^ (z >> 32));
        }
    }

    // FNV-1a over the characters of text. Gives a stable stream id from a name or hierarchy path.
    public static int HashText(string text)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
