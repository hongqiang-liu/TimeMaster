using System.Windows;

namespace TimeMaster;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindow(SettingsService settingsService, AppSettings currentSettings)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _viewModel = new SettingsWindowViewModel(currentSettings.Clone());
        DataContext = _viewModel;
    }

    private void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddReminder();
    }

    private void RemoveReminder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RemoveSelectedReminder();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildSettings(out var settings, out var errorMessage))
        {
            MessageBox.Show(this, errorMessage, "设置校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _settingsService.Save(settings);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
