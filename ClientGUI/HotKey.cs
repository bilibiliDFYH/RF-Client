using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClientGUI
{

    public delegate void HotkeyEventHandler(int HotKeyId);

    public class HotKey : IMessageFilter
    {
        [DllImport("user32.dll")]
        private static extern UInt32 RegisterHotKey(IntPtr hWnd, UInt32 id, UInt32 fsModifiers, UInt32 vk);

        [DllImport("user32.dll")]
        private static extern UInt32 UnregisterHotKey(IntPtr hWnd, UInt32 id);

        [DllImport("kernel32.dll")]
        private static extern UInt32 GlobalAddAtom(String lpString);

        [DllImport("kernel32.dll")]
        private static extern UInt32 GlobalDeleteAtom(UInt32 nAtom);

        public enum HotkeyModifiers { Alt = 0x1, Ctrl = 0x2, Shift = 0x4, Win = 0x8 };

        private IntPtr windowHandle { get; set; } = IntPtr.Zero;

        private List<uint> lstHotkeyId = null;

        public event HotkeyEventHandler OnHotkey;

        public uint this[int nIndex]
        {
            get => lstHotkeyId[nIndex];
        }

        public HotKey()
        {
            lstHotkeyId = new List<uint>();
        }

        public HotKey(IntPtr hWnd): this()
        {
            this.windowHandle = hWnd;
            Application.AddMessageFilter(this);
        }

        ~ HotKey()
        {
            Application.RemoveMessageFilter(this);
            Clear();
        }

        public bool Regist(uint fsModifiers, uint vk, string strFunc = "")
        {
            if (IntPtr.Zero == windowHandle)
                return false;

            string strID = !string.IsNullOrEmpty(strFunc) ? strFunc : lstHotkeyId.Count.ToString();
            uint hotId = GlobalAddAtom(strID) - 0xC000;
            uint nRet = RegisterHotKey(windowHandle, hotId , fsModifiers, vk);
            if(nRet > 0)
                lstHotkeyId.Add(hotId);
            return nRet > 0;
        }

        public void UnRegist(int nIndex)
        {
            if (IntPtr.Zero == windowHandle || 0 == lstHotkeyId.Count)
                return;
            UnregisterHotKey(windowHandle, lstHotkeyId[nIndex]);
        }

        public void Clear()
        {
            if (IntPtr.Zero == windowHandle || 0 == lstHotkeyId.Count)
                return;
            for (int i = 0; i < lstHotkeyId.Count; i++)
            {
                UnregisterHotKey(windowHandle, lstHotkeyId[i]);
            }
            lstHotkeyId.Clear();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 786)
            {
                foreach (var id in lstHotkeyId)
                {
                    if (m.WParam.ToInt32() == id)
                    {
                        OnHotkey?.Invoke((int)m.WParam);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
