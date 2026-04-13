using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeMaster;

public sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _sourceSettings;
    private bool _startWithWindows;
    private bool _enableMouseThrough;
    private string _selectedClockSizeKey;
    private string _selectedClockColorSchemeKey;
    private ReminderEditorItem? _selectedReminder;

    public SettingsWindowViewModel(AppSettings sourceSettings)
    {
        _sourceSettings = sourceSettings;
        _startWithWindows = sourceSettings.StartWithWindows;
        _enableMouseThrough = sourceSettings.EnableMouseThrough;

        _selectedClockSizeKey = ClockSizePresets.IsValidKey(sourceSettings.ClockSizePreset)
            ? sourceSettings.ClockSizePreset
            : ClockSizePresets.MediumKey;

        _selectedClockColorSchemeKey = ClockColorSchemes.IsValidKey(sourceSettings.ClockColorScheme)
            ? sourceSettings.ClockColorScheme
            : ClockColorSchemes.DarkKey;

        Reminders = new ObservableCollection<ReminderEditorItem>(
            sourceSettings.Reminders.Select(static item => ReminderEditorItem.FromReminder(item)));

        if (Reminders.Count > 0)
        {
            SelectedReminder = Reminders[0];
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }
    public bool EnableMouseThrough
    {
        get => _enableMouseThrough;
        set => SetProperty(ref _enableMouseThrough, value);
    }

    public bool IsSizeSmall
    {
        get => _selectedClockSizeKey == ClockSizePresets.SmallKey;
        set
        {
            if (value)
            {
                SetClockSizePreset(ClockSizePresets.SmallKey);
            }
        }
    }

    public bool IsSizeMedium
    {
        get => _selectedClockSizeKey == ClockSizePresets.MediumKey;
        set
        {
            if (value)
            {
                SetClockSizePreset(ClockSizePresets.MediumKey);
            }
        }
    }

    public bool IsSizeLarge
    {
        get => _selectedClockSizeKey == ClockSizePresets.LargeKey;
        set
        {
            if (value)
            {
                SetClockSizePreset(ClockSizePresets.LargeKey);
            }
        }
    }

    public bool IsSizeExtraLarge
    {
        get => _selectedClockSizeKey == ClockSizePresets.ExtraLargeKey;
        set
        {
            if (value)
            {
                SetClockSizePreset(ClockSizePresets.ExtraLargeKey);
            }
        }
    }

    public bool IsColorDark
    {
        get => _selectedClockColorSchemeKey == ClockColorSchemes.DarkKey;
        set
        {
            if (value)
            {
                SetClockColorScheme(ClockColorSchemes.DarkKey);
            }
        }
    }

    public bool IsColorLight
    {
        get => _selectedClockColorSchemeKey == ClockColorSchemes.LightKey;
        set
        {
            if (value)
            {
                SetClockColorScheme(ClockColorSchemes.LightKey);
            }
        }
    }

    public ObservableCollection<ReminderEditorItem> Reminders { get; }

    public ReminderEditorItem? SelectedReminder
    {
        get => _selectedReminder;
        set => SetProperty(ref _selectedReminder, value);
    }

    public void AddReminder()
    {
        var item = new ReminderEditorItem
        {
            Name = $"提醒{Reminders.Count + 1}",
            Time = "23:50:00",
            IsActive = true,
            AutoCloseTip = true,
            CountdownSeconds = 60,
            TipLine1 = "还有{0}秒就到点了",
            TipLine2 = "请注意休息"
        };

        Reminders.Add(item);
        SelectedReminder = item;
    }

    public void RemoveSelectedReminder()
    {
        if (SelectedReminder is null)
        {
            return;
        }

        var removingItem = SelectedReminder;
        var index = Reminders.IndexOf(removingItem);
        Reminders.Remove(removingItem);

        if (Reminders.Count == 0)
        {
            SelectedReminder = null;
            return;
        }

        var nextIndex = Math.Min(index, Reminders.Count - 1);
        SelectedReminder = Reminders[nextIndex];
    }

    public bool TryBuildSettings(out AppSettings settings, out string errorMessage)
    {
        errorMessage = string.Empty;
        settings = _sourceSettings.Clone();

        if (!ClockSizePresets.IsValidKey(_selectedClockSizeKey))
        {
            errorMessage = "请选择有效的时间窗大小预设。";
            return false;
        }

        if (!ClockColorSchemes.IsValidKey(_selectedClockColorSchemeKey))
        {
            errorMessage = "请选择有效的时间窗配色。";
            return false;
        }

        if (Reminders.Count == 0)
        {
            errorMessage = "至少保留一个提醒任务。";
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reminderList = new List<ReminderSetting>();

        for (var i = 0; i < Reminders.Count; i++)
        {
            var item = Reminders[i];

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errorMessage = $"第 {i + 1} 条提醒的名称不能为空。";
                return false;
            }

            if (!names.Add(item.Name.Trim()))
            {
                errorMessage = $"提醒名称重复: {item.Name}";
                return false;
            }

            if (!TimeSpan.TryParse(item.Time, out _))
            {
                errorMessage = $"第 {i + 1} 条提醒时间格式无效，请使用 HH:mm:ss。";
                return false;
            }

            if (item.CountdownSeconds < 1 || item.CountdownSeconds > 3600)
            {
                errorMessage = $"第 {i + 1} 条提醒倒计时范围应为 1 - 3600 秒。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.TipLine1) || !item.TipLine1.Contains("{0}", StringComparison.Ordinal))
            {
                errorMessage = $"第 {i + 1} 条提醒的文案1必须包含 {{0}} 占位符。";
                return false;
            }

            reminderList.Add(item.ToReminder());
        }

        var preset = ClockSizePresets.GetByKey(_selectedClockSizeKey);

        settings.StartWithWindows = StartWithWindows;
        settings.EnableMouseThrough = EnableMouseThrough;
        settings.ClockSizePreset = preset.Key;
        settings.ClockColorScheme = _selectedClockColorSchemeKey;
        settings.ClockWidth = preset.Width;
        settings.ClockHeight = preset.Height;
        settings.Reminders = reminderList;
        return true;
    }

    private void SetClockSizePreset(string presetKey)
    {
        if (_selectedClockSizeKey == presetKey)
        {
            return;
        }

        _selectedClockSizeKey = presetKey;
        OnPropertyChanged(nameof(IsSizeSmall));
        OnPropertyChanged(nameof(IsSizeMedium));
        OnPropertyChanged(nameof(IsSizeLarge));
        OnPropertyChanged(nameof(IsSizeExtraLarge));
    }

    private void SetClockColorScheme(string schemeKey)
    {
        if (_selectedClockColorSchemeKey == schemeKey)
        {
            return;
        }

        _selectedClockColorSchemeKey = schemeKey;
        OnPropertyChanged(nameof(IsColorDark));
        OnPropertyChanged(nameof(IsColorLight));
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ReminderEditorItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _time = string.Empty;
    private bool _isActive;
    private bool _autoCloseTip;
    private int _countdownSeconds;
    private string _tipLine1 = string.Empty;
    private string _tipLine2 = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool AutoCloseTip
    {
        get => _autoCloseTip;
        set => SetProperty(ref _autoCloseTip, value);
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set => SetProperty(ref _countdownSeconds, value);
    }

    public string TipLine1
    {
        get => _tipLine1;
        set => SetProperty(ref _tipLine1, value);
    }

    public string TipLine2
    {
        get => _tipLine2;
        set => SetProperty(ref _tipLine2, value);
    }

    public ReminderSetting ToReminder()
    {
        return new ReminderSetting
        {
            Name = Name.Trim(),
            Time = Time.Trim(),
            IsActive = IsActive,
            AutoCloseTip = AutoCloseTip,
            CountdownSeconds = CountdownSeconds,
            TipLine1 = TipLine1.Trim(),
            TipLine2 = TipLine2.Trim()
        };
    }

    public static ReminderEditorItem FromReminder(ReminderSetting reminder)
    {
        return new ReminderEditorItem
        {
            Name = reminder.Name,
            Time = reminder.Time,
            IsActive = reminder.IsActive,
            AutoCloseTip = reminder.AutoCloseTip,
            CountdownSeconds = reminder.CountdownSeconds,
            TipLine1 = reminder.TipLine1,
            TipLine2 = reminder.TipLine2
        };
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

