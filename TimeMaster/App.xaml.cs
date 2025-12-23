using Hardcodet.Wpf.TaskbarNotification;
using System.Configuration;
using System.Data;
using System.Windows;

namespace TimeMaster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 创建托盘图标视图模型
            var viewModel = new NotifyIconViewModel();
            // 获取资源中定义的托盘图标
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.DataContext = viewModel;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); // 退出时释放资源
            base.OnExit(e);
        }
    }
}
