using System;
using System.Runtime.InteropServices;

namespace ClientGUI
{
    public class WindowControl
    {
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        private static IntPtr _wndHandle = IntPtr.Zero;

        public static IntPtr WindowHandle { get => _wndHandle; set => _wndHandle = value; }

        public static bool DisplayWindow(uint nShow = 0)
        {
            if(IntPtr.Zero == _wndHandle)
                return false;
            ShowWindow(_wndHandle, nShow);
            return true;
        }

        public static IntPtr? FindWindow(string strName)
        {
            return FindWindow(null, strName);
        }

        public static bool DisplayWindow(IntPtr handle, uint nShow = 0)
        {
            if (IntPtr.Zero == handle)
                return false;
            ShowWindow(handle, nShow);
            return true;
        }
    }
}
