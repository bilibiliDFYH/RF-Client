using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ra2Client.DXGUI.Generic
{
    public class ComputerUniqueHelper
    {
        public static string GetComputerUUID()
        {
            //var uuid = GetSmBIOSUUID();
            //if (string.IsNullOrWhiteSpace(uuid))
            //{
                var cpuID = GetCPUID();
                var biosSerialNumber = GetBIOSSerialNumber();
                var diskSerialNumber = GetDiskDriveSerialNumber();
                var uuid = $"{cpuID}__{biosSerialNumber}__{diskSerialNumber}";
            //}
            return uuid;
        }
#nullable enable
        private static string? GetSmBIOSUUID()
        {
            var cmd = "wmic csproduct get UUID";
            return ExecuteCMD(cmd, output =>
            {
                string? uuid = GetTextAfterSpecialText(output, "UUID");
                if (uuid == "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                {
                    uuid = null;
                }
                return uuid;
            });
        }
        private static string? GetCPUID()
        {
            var cmd = "wmic cpu get processorid";
            return ExecuteCMD(cmd, output =>
            {
                var cpuid = GetTextAfterSpecialText(output, "ProcessorId");
                return cpuid;
            });
        }

        private static string? GetBIOSSerialNumber()
        {
            var cmd = "wmic bios get serialnumber";
            return ExecuteCMD(cmd, output =>
            {
                var serialNumber = GetTextAfterSpecialText(output, "SerialNumber");
                return serialNumber;
            });
        }

        private static string? GetDiskDriveSerialNumber()
        {
            var cmd = "wmic diskdrive get serialnumber";
            return ExecuteCMD(cmd, output =>
            {
                var serialNumber = GetTextAfterSpecialText(output, "SerialNumber");
                return serialNumber;
            });
        }
        private static string? GetTextAfterSpecialText(string fullText, string specialText)
        {
            if (string.IsNullOrWhiteSpace(fullText) || string.IsNullOrWhiteSpace(specialText))
            {
                return null;
            }
            string? lastText = null;
            var idx = fullText.LastIndexOf(specialText);
            if (idx > 0)
            {
                lastText = fullText.Substring(idx + specialText.Length).Trim();
            }
            return lastText;
        }
        private static string? ExecuteCMD(string cmd, Func<string, string?> filterFunc)
        {
            using var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
            process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            process.StartInfo.CreateNoWindow = true;//不显示程序窗口
            process.Start();//启动程序
            process.StandardInput.WriteLine(cmd + " &exit");
            process.StandardInput.AutoFlush = true;
            //获取cmd窗口的输出信息
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            return filterFunc(output);
        }
#nullable restore
    }

}
