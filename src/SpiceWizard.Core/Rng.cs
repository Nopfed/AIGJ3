namespace SpiceWizard.Core;

/// <summary>Small xorshift generator whose whole state is one serialisable number.</summary>
public sealed class Rng
{
    public ulong State { get; set; }

    public Rng() : this(12345) { }
    public Rng(ulong seed) { State = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }

    public uint NextUInt()
    {
        ulong x = State;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        State = x;
        return (uint)(x >> 32);
    }

    /// <summary>Uniform integer in [0, max).</summary>
    public int Next(int max) => max <= 0 ? 0 : (int)(NextUInt() % (uint)max);

    /// <summary>Uniform integer in [min, max].</summary>
    public int Range(int min, int max) => min + Next(max - min + 1);

    public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];
}
