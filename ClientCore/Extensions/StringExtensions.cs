using System;

namespace ClientCore.Extensions
{
    public static class StringExtensions
    {
        public static string GetLink(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // 支持的协议列表
            string[] protocols = {"http://", "https://", "ftp://", "sftp://", "ws://", "wss://"};
            int index = -1;

            foreach (var protocol in protocols)
            {
                index = text.IndexOf(protocol, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                    break;
            }

            if (index == -1)
                return null; // 未找到链接

            string link = text.Substring(index);
            return link.Split(' ')[0]; // 截取链接，去除后续单词
        }
    }
}