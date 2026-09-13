namespace SpiceWizard.Core;

public sealed class Progression
{
    public int Level { get; set; } = 1;
    public int Xp { get; set; }

    public bool IsMaster => Level >= Balance.MaxLevel;
    public int XpToNext => IsMaster ? 0 : Balance.XpToNext(Level);
    public double Fraction => IsMaster ? 1.0 : (double)Xp / XpToNext;

    /// <summary>Adds XP and returns how many levels were gained.</summary>
    public int AddXp(int amount)
    {
        if (IsMaster) return 0;
        Xp += amount;
        int gained = 0;
        while (!IsMaster && Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            gained++;
        }
        if (IsMaster) Xp = 0;
        return gained;
    }

    public static string Title(int level) => level switch
    {
        >= Balance.MaxLevel => "Master Spice Wizard",
        >= 16 => "Grand Saucier",
        >= 11 => "Cauldron Adept",
        >= 6 => "Pepper Apprentice",
        _ => "Novice Spice Wizard",
    };

    /// <summary>What the next level will unlock, for tooltips. Empty when nothing notable.</summary>
    public static string UnlockAt(int level)
    {
        var parts = new List<string>();
        foreach (var r in RecipeBook.All) if (r.UnlockLevel == level) parts.Add(r.Name + " recipe");
        if (level == Balance.BlendUnlockLevel) parts.Add("spice blends at the mortar");
        foreach (var s in Species.All) if (s.UnlockLevel == level && level > 1) parts.Add(s.Name + " seeds");
        for (int i = 0; i < Inventory.SpiceCount; i++) if (SpiceInfo.UnlockLevels[i] == level && level > 1) parts.Add(SpiceInfo.Names[i]);
        if (Garden.UnlockedPlots(level) > Garden.UnlockedPlots(level - 1)) parts.Add("garden plot");
        if (FermentShelf.UnlockedJars(level) > FermentShelf.UnlockedJars(level - 1)) parts.Add("fermenting jar");
        if (Balance.SpiceMaxAt(level) > Balance.SpiceMaxAt(level - 1)) parts.Add("+1 max spice");
        if (level == Balance.BigCrateLevel) parts.Add("a bigger crate (" + Balance.BigCrateCapacity + " bottles)");
        if (level == Balance.SecondHastenLevel) parts.Add("a second hasten spell each day");
        if (level == Balance.QuickAgeLevel) parts.Add("mash ages in " + Balance.NightsToAgeAt(level) + " nights");
        if (level == Balance.ForgivingTownLevel) parts.Add("the town forgives one more repeat");
        if (level == Balance.MaxLevel) parts.Add("the town celebrates!");
        return string.Join(", ", parts);
    }
}

public sealed class SpiceMeter
{
    public int Current { get; set; } = Balance.BaseSpiceMax;
    public int Max { get; set; } = Balance.BaseSpiceMax;

    public bool CanSpend(int n) => Current >= n;

    public bool TrySpend(int n)
    {
        if (!CanSpend(n)) return false;
        Current -= n;
        return true;
    }

    public void Restore(int n) => Current = Math.Min(Max, Current + n);
    public void Refill() => Current = Max;

    public void SetMaxForLevel(int level)
    {
        Max = Balance.SpiceMaxAt(level);
        Current = Math.Min(Current, Max);
    }
}

public sealed class GameClock
{
    public int Day { get; set; } = 1;
    /// <summary>Minutes since midnight, 06:00 to 22:00.</summary>
    public double Minute { get; set; } = Balance.DayStartMinute;

    public int Week => (Day - 1) / 7 + 1;
    public int DayOfWeek => (Day - 1) % 7 + 1;
    public bool IsNightfall => Minute >= Balance.DayEndMinute;

    /// <summary>0 at dawn, 1 at nightfall.</summary>
    public double DayFraction => Math.Clamp((Minute - Balance.DayStartMinute) / (Balance.DayEndMinute - Balance.DayStartMinute), 0, 1);

    public void Advance(double seconds)
    {
        double minutesPerSecond = (Balance.DayEndMinute - Balance.DayStartMinute) / Balance.DayLengthSeconds;
        Minute = Math.Min(Balance.DayEndMinute, Minute + seconds * minutesPerSecond);
    }

    public void NewDay()
    {
        Day++;
        Minute = Balance.DayStartMinute;
    }

    public string TimeText()
    {
        int m = (int)Minute;
        return $"{m / 60:00}:{m % 60:00}";
    }
}
