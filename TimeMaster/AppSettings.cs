using System.Globalization;

namespace TimeMaster;

public sealed class AppSettings
{
    public const double DefaultClockWidth = 156;
    public const double DefaultClockHeight = 48;

    public bool StartWithWindows { get; set; }
    public bool EnableMouseThrough { get; set; } = true;
    public string ClockSizePreset { get; set; } = ClockSizePresets.MediumKey;
    public string ClockColorScheme { get; set; } = ClockColorSchemes.DarkKey;

    // Legacy fields are kept for backward compatibility with old settings files.
    public double ClockWidth { get; set; } = DefaultClockWidth;
    public double ClockHeight { get; set; } = DefaultClockHeight;

    public double ClockLeft { get; set; } = double.NaN;
    public double ClockTop { get; set; } = double.NaN;
    public List<ReminderSetting> Reminders { get; set; } = new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            StartWithWindows = StartWithWindows,
            EnableMouseThrough = EnableMouseThrough,
            ClockSizePreset = ClockSizePreset,
            ClockColorScheme = ClockColorScheme,
            ClockWidth = ClockWidth,
            ClockHeight = ClockHeight,
            ClockLeft = ClockLeft,
            ClockTop = ClockTop,
            Reminders = Reminders.Select(static item => item.Clone()).ToList()
        };
    }
}

public sealed class ClockSizePresetOption
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required double FontSize { get; init; }
}

public static class ClockSizePresets
{
    public const string SmallKey = "small";
    public const string MediumKey = "medium";
    public const string LargeKey = "large";
    public const string ExtraLargeKey = "xlarge";

    private static readonly IReadOnlyList<ClockSizePresetOption> Presets = new List<ClockSizePresetOption>
    {
        new() { Key = SmallKey, DisplayName = "小", Width = 130, Height = 40, FontSize = 30 },
        new() { Key = MediumKey, DisplayName = "中", Width = 156, Height = 48, FontSize = 36 },
        new() { Key = LargeKey, DisplayName = "大", Width = 208, Height = 64, FontSize = 48 },
        new() { Key = ExtraLargeKey, DisplayName = "超大", Width = 260, Height = 80, FontSize = 60 }
    };

    public static IReadOnlyList<ClockSizePresetOption> All => Presets;

    public static bool IsValidKey(string? key)
    {
        return Presets.Any(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static ClockSizePresetOption GetByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Presets[1];
        }

        return Presets.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? Presets[1];
    }

    public static ClockSizePresetOption FromLegacySize(double width, double height)
    {
        if (double.IsNaN(width) || double.IsNaN(height))
        {
            return Presets[1];
        }

        return Presets
            .OrderBy(item => Math.Abs(item.Width - width) + Math.Abs(item.Height - height))
            .First();
    }
}

public sealed class ClockColorSchemeOption
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string BackgroundColor { get; init; }
    public required string ForegroundColor { get; init; }
}

public static class ClockColorSchemes
{
    public const string DarkKey = "dark";
    public const string LightKey = "light";

    private static readonly IReadOnlyList<ClockColorSchemeOption> Schemes = new List<ClockColorSchemeOption>
    {
        new() { Key = DarkKey, DisplayName = "黑底白字", BackgroundColor = "#88000000", ForegroundColor = "WhiteSmoke" },
        new() { Key = LightKey, DisplayName = "白底黑字", BackgroundColor = "#CCFFFFFF", ForegroundColor = "#111111" }
    };

    public static IReadOnlyList<ClockColorSchemeOption> All => Schemes;

    public static bool IsValidKey(string? key)
    {
        return Schemes.Any(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static ClockColorSchemeOption GetByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Schemes[0];
        }

        return Schemes.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? Schemes[0];
    }
}

public sealed class ReminderSetting
{
    public string Name { get; set; } = "Reminder";
    public string Time { get; set; } = "23:50:00";
    public bool IsActive { get; set; } = true;
    public bool AutoCloseTip { get; set; } = true;
    public int CountdownSeconds { get; set; } = 60;
    public string TipLine1 { get; set; } = "还有{0}秒就到点了";
    public string TipLine2 { get; set; } = "请注意休息";

    public ReminderSetting Clone()
    {
        return new ReminderSetting
        {
            Name = Name,
            Time = Time,
            IsActive = IsActive,
            AutoCloseTip = AutoCloseTip,
            CountdownSeconds = CountdownSeconds,
            TipLine1 = TipLine1,
            TipLine2 = TipLine2
        };
    }

    public string GetCountdownTime()
    {
        if (!TimeSpan.TryParse(Time, CultureInfo.InvariantCulture, out var triggerTime))
        {
            throw new FormatException($"时间格式无效: {Time}");
        }

        var seconds = CountdownSeconds <= 0 ? 60 : CountdownSeconds;
        var countdownTime = triggerTime.Subtract(TimeSpan.FromSeconds(seconds));
        if (countdownTime.TotalSeconds < 0)
        {
            countdownTime = countdownTime.Add(TimeSpan.FromDays(1));
        }

        return countdownTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }
}
