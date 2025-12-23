using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;

namespace TimeMaster
{
    public class NotifyIconViewModel
    {
        /// <summary>
        /// 显示主窗口命令
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                    }
                };
            }
        }
        
        /// <summary>
        /// 退出应用程序命令
        /// </summary>
        public ICommand RestartApplicationCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => 
                    {
                        // 获取当前应用程序的可执行文件路径
                        string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
                        // 启动新实例
                        Process.Start(new ProcessStartInfo(applicationPath)
                        {
                            UseShellExecute = true
                        });
                        // 关闭当前实例
                        Application.Current.Shutdown();
                    }
                };
            }
        }

        /// <summary>
        /// 退出应用程序命令
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.Shutdown()
                };
            }
        }

        /// <summary>
        /// 退出应用程序命令
        /// </summary>
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
        public Action CommandAction { get; set; }
        public Func<bool> CanExecuteFunc { get; set; }

        public void Execute(object parameter)
        {
            CommandAction();
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
