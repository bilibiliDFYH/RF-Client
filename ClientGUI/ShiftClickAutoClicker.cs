using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ClientGUI
{
    public sealed class ShiftClickAutoClicker
    {
        private static readonly Lazy<ShiftClickAutoClicker> _instance = new(() => new ShiftClickAutoClicker());
        public static ShiftClickAutoClicker Instance => _instance.Value;

        private Thread _monitorThread;
        private bool _monitoring = false;
        private bool _wasLeftDown = false;

        private const int VK_SHIFT = 0x10;
        private const int VK_LBUTTON = 0x01;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private ShiftClickAutoClicker() { }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        /// <summary>
        /// 启动监听
        /// </summary>
        public void Start()
        {
            if (_monitoring) return;

            _monitoring = true;
            _monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true
            };
            _monitorThread.Start();

            Console.WriteLine("ShiftClickAutoClicker 已开始监听...");
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            _monitoring = false;
            _monitorThread?.Join();
            Console.WriteLine("ShiftClickAutoClicker 已停止监听。");
        }

        private void MonitorLoop()
        {
            while (_monitoring)
            {
                bool shiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                bool leftDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

                if (shiftDown && leftDown && !_wasLeftDown)
                {
                //    Console.WriteLine("检测到 Shift + 鼠标左键，自动点击 5 次...");
                    AutoClick(2);
                }

                _wasLeftDown = leftDown;
                Thread.Sleep(10);
            }
        }

        private void AutoClick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(50);
            }
        }
    }
}
