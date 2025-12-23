using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml;

namespace TimeMaster
{
    public partial class CountdownWindow : Window
    {
        public event Action CancelClicked;
        private readonly System.Timers.Timer _updateTimer = new System.Timers.Timer(1000);
        private int _secondsLeft;
        private bool _autoClose = true;


        public CountdownWindow(TimerElement timer)
        {
            InitializeComponent();
            var match = Regex.Match(timer.TipLine1, @"^(.*)\{0\}(.*)$");
            this.tipLine1L.Text = match.Groups[1].Value; // "还有"
            this.tipLine1R.Text = match.Groups[2].Value; // "秒就到点了"
            this.tipLine2.Text = timer.TipLine2;
            _secondsLeft = timer.CountdownSeconds;
            _autoClose = timer.AutoCloseTip;
            this.Init();

        }

        public CountdownWindow(int seconds, bool autoClose)
        {
            InitializeComponent();
            _secondsLeft = seconds;
            _autoClose = autoClose;
            this.Init();

        }

        public CountdownWindow() : this(15, true)
        {
        }

        private void Init()
        {  
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Maximized;
            UpdateCountdownDisplay();
            // 设置更新计时器
            _updateTimer.Elapsed += (s, e) => Dispatcher.Invoke(() =>
            {
                _secondsLeft--;
                UpdateCountdownDisplay();

                if (_secondsLeft <= 0)
                {
                    _updateTimer.Stop();
                    if (this._autoClose)
                    {
                        this.Close();
                    }
                }
            });
            _updateTimer.Start();
        }



        private Brush _background1 = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0));
        private Brush _background2 = new SolidColorBrush(Color.FromArgb(0x60, 0, 0, 0));
        private Brush _background3 = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
        private Brush _background0 = new SolidColorBrush(Color.FromArgb(0x00, 0, 0, 0));
        private void UpdateCountdownDisplay()
        {
            txtCountdown.Text = _secondsLeft.ToString();
            // 根据剩余时间改变颜色
            if (_secondsLeft <= 10)
            {
                txtCountdown.Foreground = Brushes.Red;
                this.Background = _background1;
            }
            else if (_secondsLeft <= 20)
            {
                txtCountdown.Foreground = Brushes.Red;
                this.Background = _background2;
            }
            else if (_secondsLeft <= 30)
            {
                txtCountdown.Foreground = Brushes.Orange;
                this.Background = _background3;
            }
            else
            {
                txtCountdown.Foreground = Brushes.White;
                this.Background = _background0;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            this.CancelClicked?.Invoke();
            this.Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 实现鼠标穿透
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }
    }
}
