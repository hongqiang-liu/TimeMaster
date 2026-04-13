using System.Globalization;
using System.IO;
using System.Text.Json;

namespace TimeMaster;

public sealed class SettingsService
{
    private const string SettingsFolderName = "TimeMaster";
    private const string SettingsFileName = "clock_settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly IReadOnlyList<ReminderSetting> BuiltInReminders =
    [
        new ReminderSetting
        {
            Name = "GoToBed",
            Time = "23:50:00",
            IsActive = true,
            AutoCloseTip = false,
            CountdownSeconds = 60,
            TipLine1 = "还有{0}秒就凌晨了",
            TipLine2 = "不要熬夜，赶快上床睡觉"
        },
        new ReminderSetting
        {
            Name = "MorningMeeting",
            Time = "09:30:00",
            IsActive = false,
            AutoCloseTip = true,
            CountdownSeconds = 60,
            TipLine1 = "会议即将开始，还有{0}秒",
            TipLine2 = "请准备会议材料"
        }
    ];

    private readonly string _settingsPath;
    private readonly string _legacySettingsPath;

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? BuildDefaultPath();
        _legacySettingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
    }

    public AppSettings Load()
    {
        var settings = ReadSettingsFromDisk();
        if (settings is null)
        {
            settings = new AppSettings();
            ApplyLegacyWindowPosition(settings);
        }

        Normalize(settings);
        MergeBuiltInReminderDefaults(settings);
        settings.StartWithWindows = StartupManager.IsEnabled();
        return settings;
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);

        StartupManager.SetEnabled(settings.StartWithWindows);
    }

    private AppSettings? ReadSettingsFromDisk()
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyLegacyWindowPosition(AppSettings settings)
    {
        if (!File.Exists(_legacySettingsPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_legacySettingsPath);
            var legacy = JsonSerializer.Deserialize<LegacyWindowSettings>(json, JsonOptions);
            if (legacy is null)
            {
                return;
            }

            settings.ClockLeft = legacy.Left;
            settings.ClockTop = legacy.Top;
        }
        catch
        {
            // Legacy file is optional.
        }
    }

    private static void MergeBuiltInReminderDefaults(AppSettings settings)
    {
        var existing = settings.Reminders
            .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var reminder in BuiltInReminders)
        {
            if (!existing.ContainsKey(reminder.Name))
            {
                settings.Reminders.Add(reminder.Clone());
            }
        }

        if (settings.Reminders.Count == 0)
        {
            settings.Reminders.Add(new ReminderSetting());
        }
    }

    private static void Normalize(AppSettings settings)
    {
        if (!ClockSizePresets.IsValidKey(settings.ClockSizePreset))
        {
            var fromLegacy = ClockSizePresets.FromLegacySize(settings.ClockWidth, settings.ClockHeight);
            settings.ClockSizePreset = fromLegacy.Key;
        }

        var sizePreset = ClockSizePresets.GetByKey(settings.ClockSizePreset);
        settings.ClockWidth = sizePreset.Width;
        settings.ClockHeight = sizePreset.Height;

        if (!ClockColorSchemes.IsValidKey(settings.ClockColorScheme))
        {
            settings.ClockColorScheme = ClockColorSchemes.DarkKey;
        }

        settings.Reminders ??= new List<ReminderSetting>();

        for (var i = 0; i < settings.Reminders.Count; i++)
        {
            var reminder = settings.Reminders[i];

            if (string.IsNullOrWhiteSpace(reminder.Name))
            {
                reminder.Name = $"提醒{i + 1}";
            }

            if (!TimeSpan.TryParse(reminder.Time, CultureInfo.InvariantCulture, out _))
            {
                reminder.Time = "23:50:00";
            }

            if (reminder.CountdownSeconds < 1 || reminder.CountdownSeconds > 3600)
            {
                reminder.CountdownSeconds = 60;
            }

            if (string.IsNullOrWhiteSpace(reminder.TipLine1))
            {
                reminder.TipLine1 = "还有{0}秒就到点了";
            }

            reminder.TipLine2 ??= string.Empty;
        }
    }

    private static string BuildDefaultPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, SettingsFolderName, SettingsFileName);
    }

    private sealed class LegacyWindowSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
