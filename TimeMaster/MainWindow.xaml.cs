using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace TimeMaster
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly SettingsService _settingsService;

        private AppSettings _settings = new();
        private List<ReminderSetting> _tasks = new();

        private bool _isDragging;
        private Point _startPoint;
        private bool _isMouseTransparent;
        private bool _allowClose;
        private bool _isMouseThroughEnabled = true;
        private bool _isHovering;
        private bool _isLeftCtrlPressed;

        public MainWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();
            ReloadSettings(applyPosition: true);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateTime();

            KeyDown += Window_KeyDown;
            KeyUp += Window_KeyUp;
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
            MouseMove += Window_MouseMove;
            MouseLeftButtonUp += Window_MouseLeftButtonUp;
            MouseEnter += Window_MouseEnter;
            MouseLeave += Window_MouseLeave;

            Closing += (_, e) =>
            {
                if (_allowClose)
                {
                    return;
                }

                e.Cancel = true;
                Hide();
            };
        }

        public void ShowClockWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        public void ExitApplication()
        {
            _allowClose = true;
            Close();
        }

        public void OpenSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(_settingsService, _settings)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                ReloadSettings(applyPosition: true);
                if (!IsPositionValid(Left, Top, Width, Height))
                {
                    MoveWindowToDefaultPosition();
                    SaveWindowPlacement();
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            var nowText = now.ToString("HH:mm:ss");

            foreach (var timer in _tasks)
            {
                if (!timer.IsActive)
                {
                    continue;
                }

                try
                {
                    if (nowText == timer.GetCountdownTime())
                    {
                        new CountdownWindow(timer).Show();
                    }
                }
                catch
                {
                    // Ignore malformed reminder entries so a single invalid row does not stop the clock.
                }
            }

            TimeText.Text = nowText;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl && !_isLeftCtrlPressed)
            {
                _isLeftCtrlPressed = true;
                ApplyMouseThroughState();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                _isLeftCtrlPressed = false;
                ApplyMouseThroughState();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!CanInteractWithMouse())
            {
                return;
            }

            _isDragging = true;
            _startPoint = e.GetPosition(this);
            CaptureMouse();
            ApplyMouseThroughState();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(this);
                Left += currentPosition.X - _startPoint.X;
                Top += currentPosition.Y - _startPoint.Y;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                SaveWindowPlacement();
                ApplyMouseThroughState();
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isMouseTransparent)
            {
                return;
            }

            _isHovering = true;
            ApplyHoverVisual(true);
            UpdateWindowOpacity();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHovering = false;
            ApplyHoverVisual(false);
            UpdateWindowOpacity();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyMouseThroughState();
            ApplyHoverVisual(false, immediate: true);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.Stop();
        }

        private bool CanInteractWithMouse()
        {
            return !_isMouseThroughEnabled || _isLeftCtrlPressed;
        }

        private void ReloadSettings(bool applyPosition)
        {
            _settings = _settingsService.Load();
            ApplyClockSizePreset(_settings.ClockSizePreset);
            ApplyClockColorScheme(_settings.ClockColorScheme);

            _isMouseThroughEnabled = _settings.EnableMouseThrough;
            if (!_isMouseThroughEnabled)
            {
                _isLeftCtrlPressed = false;
            }

            ApplyMouseThroughState();

            if (applyPosition)
            {
                if (IsPositionValid(_settings.ClockLeft, _settings.ClockTop, Width, Height))
                {
                    Left = _settings.ClockLeft;
                    Top = _settings.ClockTop;
                }
                else
                {
                    MoveWindowToDefaultPosition();
                }
            }

            _tasks = _settings.Reminders.Select(static item => item.Clone()).ToList();
        }

        private void ApplyClockSizePreset(string presetKey)
        {
            var preset = ClockSizePresets.GetByKey(presetKey);
            Width = preset.Width;
            Height = preset.Height;
            TimeText.FontSize = preset.FontSize;

            _settings.ClockSizePreset = preset.Key;
            _settings.ClockWidth = preset.Width;
            _settings.ClockHeight = preset.Height;
        }

        private void ApplyClockColorScheme(string schemeKey)
        {
            var scheme = ClockColorSchemes.GetByKey(schemeKey);
            ClockBorder.Background = BuildBrush(scheme.BackgroundColor, Color.FromArgb(0x88, 0, 0, 0));
            TimeText.Foreground = BuildBrush(scheme.ForegroundColor, Colors.WhiteSmoke);
            _settings.ClockColorScheme = scheme.Key;
        }

        private void ApplyMouseThroughState()
        {
            var shouldTransparent = _isMouseThroughEnabled && !_isLeftCtrlPressed && !_isDragging;
            SetMouseTransparent(shouldTransparent);

            if (shouldTransparent)
            {
                _isHovering = false;
                ApplyHoverVisual(false);
            }

            UpdateWindowOpacity();
        }

        private void SetMouseTransparent(bool transparent)
        {
            if (_isMouseTransparent == transparent)
            {
                return;
            }

            _isMouseTransparent = transparent;

            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var extendedStyle = WindowsServices.GetWindowLong(hwnd, WindowsServices.GWL_EXSTYLE);

            if (transparent)
            {
                WindowsServices.SetWindowLong(hwnd, WindowsServices.GWL_EXSTYLE,
                    extendedStyle | WindowsServices.WS_EX_TRANSPARENT);
            }
            else
            {
                WindowsServices.SetWindowLong(hwnd, WindowsServices.GWL_EXSTYLE,
                    extendedStyle & ~WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void ApplyHoverVisual(bool isHovering, bool immediate = false)
        {
            var targetScale = isHovering ? 1.04 : 1.0;
            var targetShadowOpacity = isHovering ? 0.45 : 0.20;
            var targetShadowBlur = isHovering ? 16.0 : 8.0;

            AnimateDouble(ClockScaleTransform, ScaleTransform.ScaleXProperty, targetScale, immediate);
            AnimateDouble(ClockScaleTransform, ScaleTransform.ScaleYProperty, targetScale, immediate);
            AnimateDouble(ClockShadowEffect, DropShadowEffect.OpacityProperty, targetShadowOpacity, immediate);
            AnimateDouble(ClockShadowEffect, DropShadowEffect.BlurRadiusProperty, targetShadowBlur, immediate);
        }

        private void UpdateWindowOpacity()
        {
            var targetOpacity = _isMouseTransparent ? 0.78 : (_isHovering ? 1.0 : 0.93);
            AnimateDouble(this, OpacityProperty, targetOpacity, immediate: false);
        }

        private static void AnimateDouble(DependencyObject target, DependencyProperty property, double toValue, bool immediate)
        {
            if (immediate)
            {
                target.SetValue(property, toValue);
                return;
            }

            var animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            switch (target)
            {
                case UIElement uiElement:
                    uiElement.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
                    break;
                case Animatable animatable:
                    animatable.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
                    break;
            }
        }

        private static SolidColorBrush BuildBrush(string colorText, Color fallbackColor)
        {
            try
            {
                var converted = ColorConverter.ConvertFromString(colorText);
                if (converted is Color color)
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
            }
            catch
            {
                // Fallback below.
            }

            var fallbackBrush = new SolidColorBrush(fallbackColor);
            fallbackBrush.Freeze();
            return fallbackBrush;
        }

        private void SaveWindowPlacement()
        {
            _settings.ClockLeft = Left;
            _settings.ClockTop = Top;

            try
            {
                _settingsService.Save(_settings);
            }
            catch
            {
                // Keep runtime behavior unaffected when local persistence fails.
            }
        }

        private void MoveWindowToDefaultPosition()
        {
            Left = SystemParameters.WorkArea.Width - Width - 20;
            Top = 20;
        }

        private bool IsPositionValid(double left, double top, double width, double height)
        {
            if (double.IsNaN(left) || double.IsNaN(top))
            {
                return false;
            }

            const double margin = 5;
            foreach (var screen in WindowsServices.GetAllScreensWorkArea())
            {
                if (left >= screen.Left + margin &&
                    left + width <= screen.Right - margin &&
                    top >= screen.Top + margin &&
                    top + height <= screen.Bottom - margin)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

