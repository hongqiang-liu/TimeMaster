using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TimeMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private bool _isDragging = false;
        private Point _startPoint;
        private const string SETTINGS_FILE = "clock_settings.json";
        private bool _isMouseTransparent = true;
        private TimerCollection tasks;
        public MainWindow()
        {
            InitializeComponent();

            // 加载保存的位置
            LoadWindowPosition();

            tasks = TimerConfigHelper.GetTimers();
            foreach (TimerElement timer in tasks) 
            {
                Console.WriteLine(timer);
                Console.WriteLine(timer.GetCountdownTime());
            }

            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateTime();

            // 键盘和鼠标事件
            this.KeyDown += Window_KeyDown;
            this.KeyUp += Window_KeyUp;
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
            this.MouseMove += Window_MouseMove;
            this.MouseLeftButtonUp += Window_MouseLeftButtonUp;
            // 窗口关闭时最小化到托盘而不是退出
            this.Closing += (s, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            string now = DateTime.Now.ToString("HH:mm:ss");
            foreach (TimerElement timer in tasks) 
            {
                if (timer.IsActive && now == timer.GetCountdownTime())
                { 
                    CountdownWindow countdownWindow = new CountdownWindow(timer);
                    countdownWindow.Show();
                }
            }
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 按下Ctrl键时临时禁用鼠标穿透
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                SetMouseTransparent(false);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // 释放Ctrl键时恢复鼠标穿透
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && !_isDragging)
            {
                SetMouseTransparent(true);
            }
        }


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " Window_MouseLeftButtonDown");
            // 按下Ctrl键时开始拖动
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isDragging = true;
                _startPoint = e.GetPosition(this);
                this.CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " Window_MouseMove");
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                this.Left += currentPosition.X - _startPoint.X;
                this.Top += currentPosition.Y - _startPoint.Y;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();
                SaveWindowPosition(); // 保存新位置
                // 拖动结束后恢复鼠标穿透
                if (Keyboard.Modifiers != ModifierKeys.Control)
                {
                    SetMouseTransparent(true);
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 默认状态下鼠标穿透
            SetMouseTransparent(true);
        }

        private void SetMouseTransparent(bool transparent)
        {
            if (_isMouseTransparent == transparent)
                return;

            _isMouseTransparent = transparent;

            // 更新窗口外观提供视觉反馈
            if (transparent)
            {
                this.Opacity = 0.8; // 半透明
            }
            else
            {
                this.Opacity = 1.0; // 完全不透明
            }
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int extendedStyle = WindowsServices.GetWindowLong(hwnd, WindowsServices.GWL_EXSTYLE);

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

        private bool IsPositionValid(double left, double top)
        {
            // 添加5px的边距，确保窗口标题栏等不会被遮挡
            const double margin = 5;
            foreach (var screen in WindowsServices.GetAllScreensWorkArea())
            {
                if (left >= screen.Left + margin &&
                    left + this.ActualWidth <= screen.Right - margin &&
                    top >= screen.Top + margin &&
                    top + this.ActualHeight <= screen.Bottom - margin)
                {
                    return true;
                }
            }
            return false;
        }


        private void LoadWindowPosition()
        {
            if (File.Exists(SETTINGS_FILE))
            {
                try
                {
                    string json = File.ReadAllText(SETTINGS_FILE);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    // 检查位置是否在屏幕可见范围内
                    if (IsPositionValid(settings.Left, settings.Top))
                    {
                        this.Left = settings.Left;
                        this.Top = settings.Top;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载设置失败: {ex.Message}");
                }
            }
            // 默认位置：屏幕右上角
            this.Left = SystemParameters.WorkArea.Width - this.Width - 20;
            this.Top = 20;
        }

        private void SaveWindowPosition()
        {
            var settings = new WindowSettings
            {
                Left = this.Left,
                Top = this.Top
            };
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.Stop();
        }


        public class WindowSettings
        {
            public double Left { get; set; }
            public double Top { get; set; }
        }
    }
}
    