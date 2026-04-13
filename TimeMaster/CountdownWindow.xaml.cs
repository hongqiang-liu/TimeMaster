using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TimeMaster
{
    public partial class CountdownWindow : Window
    {
        public event Action? CancelClicked;

        private readonly System.Timers.Timer _updateTimer = new(1000);
        private int _secondsLeft;
        private bool _autoClose = true;

        private readonly Brush _background1 = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0));
        private readonly Brush _background2 = new SolidColorBrush(Color.FromArgb(0x60, 0, 0, 0));
        private readonly Brush _background3 = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
        private readonly Brush _background0 = new SolidColorBrush(Color.FromArgb(0x00, 0, 0, 0));

        public CountdownWindow(ReminderSetting reminder)
            : this(reminder.CountdownSeconds, reminder.AutoCloseTip)
        {
            ApplyTipText(reminder.TipLine1, reminder.TipLine2);
        }

        public CountdownWindow(int seconds, bool autoClose)
        {
            InitializeComponent();
            _secondsLeft = seconds;
            _autoClose = autoClose;
            Init();
        }

        public CountdownWindow() : this(15, true)
        {
        }

        private void Init()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            UpdateCountdownDisplay();

            _updateTimer.Elapsed += (_, _) => Dispatcher.Invoke(() =>
            {
                _secondsLeft--;
                UpdateCountdownDisplay();

                if (_secondsLeft <= 0)
                {
                    _updateTimer.Stop();
                    if (_autoClose)
                    {
                        Close();
                    }
                }
            });

            _updateTimer.Start();
        }

        private void ApplyTipText(string tipLine1, string tipLine2)
        {
            var formattedText = string.IsNullOrWhiteSpace(tipLine1) ? "还有{0}秒就到点了" : tipLine1;
            var match = Regex.Match(formattedText, @"^(.*)\{0\}(.*)$");

            if (match.Success)
            {
                tipLine1L.Text = match.Groups[1].Value;
                tipLine1R.Text = match.Groups[2].Value;
            }
            else
            {
                tipLine1L.Text = formattedText;
                tipLine1R.Text = string.Empty;
            }

            tipLine2 = string.IsNullOrWhiteSpace(tipLine2) ? "请注意休息" : tipLine2;
            this.tipLine2.Text = tipLine2;
        }

        private void UpdateCountdownDisplay()
        {
            txtCountdown.Text = _secondsLeft.ToString();

            if (_secondsLeft <= 10)
            {
                txtCountdown.Foreground = Brushes.Red;
                Background = _background1;
            }
            else if (_secondsLeft <= 20)
            {
                txtCountdown.Foreground = Brushes.Red;
                Background = _background2;
            }
            else if (_secondsLeft <= 30)
            {
                txtCountdown.Foreground = Brushes.Orange;
                Background = _background3;
            }
            else
            {
                txtCountdown.Foreground = Brushes.White;
                Background = _background0;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            CancelClicked?.Invoke();
            Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }
    }
}
