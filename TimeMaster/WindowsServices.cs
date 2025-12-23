using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;

namespace TimeMaster
{

    // 使用 Windows API 获取精确的显示器信息
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    // Windows API封装类
    public static class WindowsServices
    {

        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor([In] IntPtr hmonitor, [In] int dpiType, 
            [Out] out uint dpiX, [Out] out uint dpiY);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        public static Rect[] GetAllScreensWorkArea()
        {
            try
            {
                var screens = new List<Rect>();
                var callback = new MonitorEnumProc((IntPtr hMonitor, IntPtr hdcMonitor,
                    ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    screens.Add(new Rect(
                        lprcMonitor.Left,
                        lprcMonitor.Top,
                        lprcMonitor.Right - lprcMonitor.Left,
                        lprcMonitor.Bottom - lprcMonitor.Top));
                    return true;
                });
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
                return screens.ToArray();
            }
            catch
            {
                // 回退到虚拟屏幕信息
                return new Rect[] {
                    new Rect(
                        SystemParameters.VirtualScreenLeft,
                        SystemParameters.VirtualScreenTop,
                        SystemParameters.VirtualScreenWidth,
                        SystemParameters.VirtualScreenHeight)
                };
            }
        }

    }
}
