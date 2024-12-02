
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

namespace Localization.Tools
{
    public static class FunExtensions
    {
        /// <summary>
        /// 判断字符串是否包含中文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool CheckChineseStr(this string str)
        {
            string pat = @"[\u4e00-\u9fff]";
            Regex rg = new Regex(pat);
            Match mh = rg.Match(str);
            return mh.Success;
        }

        /// <summary>
        /// 将字典中的繁体中文值转换为简体中文
        /// </summary>
        /// <param name="dict">需要转换的字典</param>
        public static void ConvertValuesToSimplified<Tkey>(this Dictionary<Tkey, string> dict)
        {
            var keys = new List<Tkey>(dict.Keys);
            foreach (Tkey key in keys)
            {
                dict[key] = ChineseConverter.Convert(dict[key], ChineseConversionDirection.TraditionalToSimplified);
            }
        }

        /// <summary>
        /// 将文件大小转换为MB
        /// </summary>
        /// <param name="fileSizeInBytes">需要转换的文件大小</param>
        /// <param name="decimalPlaces">保留多少位小数</param>
        /// <returns></returns>
        public static string ToFileSizeString(this long fileSizeInBytes, int decimalPlaces)
        {
            double fileSizeInMB = (double)fileSizeInBytes / (1024 * 1024); // Convert bytes to megabytes
            string formatSpecifier = "0." + new string('0', decimalPlaces);
            string fileSizeFormatted = fileSizeInMB.ToString(formatSpecifier); // Format to the specified number of decimal places
            return fileSizeFormatted;
        }

        public static bool IsValidEmail(this string email)
        {
            // 定义一个正则表达式模式来验证电子邮件地址
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

            // 使用正则表达式进行匹配验证
            Regex regex = new Regex(pattern);
            return regex.IsMatch(email);
        }

        public static string ComputeHash(this string filePath)
        {
            using var hashAlgorithm = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = hashAlgorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - DateTime.UnixEpoch;
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

    }
}
