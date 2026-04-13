using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;

namespace TimeMaster
{
    public partial class App : Application
    {
        private TaskbarIcon? notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
