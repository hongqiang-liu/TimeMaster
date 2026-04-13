using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace TimeMaster
{
    public class NotifyIconViewModel
    {
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ShowClockWindow();
                        }
                    }
                };
            }
        }

        public ICommand OpenSettingsCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.OpenSettingsWindow();
                        }
                    }
                };
            }
        }

        public ICommand RestartApplicationCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        var applicationPath = Process.GetCurrentProcess().MainModule?.FileName;
                        if (string.IsNullOrWhiteSpace(applicationPath))
                        {
                            return;
                        }

                        Process.Start(new ProcessStartInfo(applicationPath)
                        {
                            UseShellExecute = true
                        });

                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ExitApplication();
                        }
                        else
                        {
                            Application.Current.Shutdown();
                        }
                    }
                };
            }
        }

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ExitApplication();
                        }
                        else
                        {
                            Application.Current.Shutdown();
                        }
                    }
                };
            }
        }

        public ICommand OpenCountdownWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => (new CountdownWindow()).Show()
                };
            }
        }
    }

    public class DelegateCommand : ICommand
    {
        public Action? CommandAction { get; set; }
        public Func<bool>? CanExecuteFunc { get; set; }

        public void Execute(object? parameter)
        {
            CommandAction?.Invoke();
        }

        public bool CanExecute(object? parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
