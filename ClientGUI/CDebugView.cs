using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ClientGUI
{
    /// <summary>
    /// 监控异常跟踪类，可在调试/发布版本下实时打印日志信息
    /// </summary>
    public class CDebugView
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void OutputDebugString(string message);

        private static string _strPrefix = string.Empty;

        public static void SetDebugName(string strName)
        {
            if (string.IsNullOrEmpty(strName))
                _strPrefix = "[ClientGUI]";
            else
                _strPrefix = string.Format("[{0}]", strName);
        }

        public static void OutputDebugInfo(string strMsg)
        {
            StringBuilder sBuff = new StringBuilder();
            sBuff.AppendFormat("{0}{1}", _strPrefix, strMsg);
            OutputDebugString(sBuff.ToString());
            Console.WriteLine(sBuff.ToString());
            sBuff.Clear();
        }

        public static void OutputDebugInfo(string strMsg, params object[] arg)
        {
            StringBuilder sBuff = new StringBuilder();
            sBuff.AppendFormat(strMsg, arg);
            OutputDebugInfo(sBuff.ToString());
            sBuff.Clear();
        }

        public static void TraceMessage(string strMsg,
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = default(int))
        {
            StringBuilder sBuff = new StringBuilder();
            sBuff.AppendFormat("{0}{1}-{2}:{3}:{4}", _strPrefix, sourceFilePath, memberName, sourceLineNumber, strMsg);
            System.Diagnostics.Debug.WriteLine(sBuff);
            sBuff.Clear();
        }
    }
}
