using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Localization.Tools
{
    public class CSF(string csfFilename)
    {

        public string csfPath = csfFilename;

        // 获取 CSF 文件内容的方法
        public Dictionary<string, string> GetCsfDictionary()
        {
            // 检查 CSF 文件是否存在，如果存在则读取它，否则返回 null
            return File.Exists(csfPath) ? ReadCSF(csfPath) : null;
        }

        // 读取 CSF 文件头部信息的方法
        static Tuple<byte[], uint, uint, uint, uint, uint> ReadHeader(BinaryReader csfFile)
        {
            byte[] headerBytes = csfFile.ReadBytes(0x18); // 读取头部的 24 个字节
            byte[] headerSubset = new byte[4];
            Array.Copy(headerBytes, headerSubset, 4); // 提取头部的前 4 个字节

            // 读取 CSF 版本号
            uint csfVersion = BitConverter.ToUInt32(headerBytes, 4);
            // 读取标签数
            uint numLabels = BitConverter.ToUInt32(headerBytes, 8);
            // 读取字符串数
            uint numStrings = BitConverter.ToUInt32(headerBytes, 12);
            // 读取未使用的字段
            uint unused = BitConverter.ToUInt32(headerBytes, 16);
            // 读取语言 ID
            uint langId = BitConverter.ToUInt32(headerBytes, 20);

            return Tuple.Create(headerSubset, csfVersion, numLabels, numStrings, unused, langId);
        }

        // 检查文件头部是否有效的方法
        static int CheckHeader(Tuple<byte[], uint, uint, uint, uint, uint> header)
        {
            byte[] fsc = header.Item1; // CSF版本号 

            // 检查文件头部标识是否为 " FSC"
            if (!ByteArrayCompare(fsc, Encoding.ASCII.GetBytes(" FSC")))
            {
                throw new InvalidDataException("This is not a valid CSF file!"); // 如果不是有效的 CSF 文件，抛出异常
            }

            uint numLabels = header.Item3; //读取标签数
            uint numStrings = header.Item4; //读取字符串数

            // 检查标签数是否等于字符串数
            if (numLabels != numStrings)
            {
                throw new InvalidDataException("Label count and string count are unequal"); // 如果不相等，抛出异常
            }

            return (int)numLabels;
        }

        // 读取 CSF 文件并返回一个字典
        static Dictionary<string, string> ReadCSF(string csfFilename)
        {
            try
            {
                //创建字典，获取键时忽略大小写
                var nameStrMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                using (var csfFile = new BinaryReader(File.OpenRead(csfFilename)))
                {
                    Tuple<byte[], uint, uint, uint, uint, uint> header = ReadHeader(csfFile);
                    int numLabels = CheckHeader(header);

                    // 遍历文件中的每个标签和字符串
                    for (int i = 0; i < numLabels; i++)
                    {
                        byte[] lblBytes = csfFile.ReadBytes(4);
                        byte[] oneBytes = csfFile.ReadBytes(4);
                        byte[] uinameLengthBytes = csfFile.ReadBytes(4);

                        string lbl = Encoding.ASCII.GetString(lblBytes);
                        int one = BitConverter.ToInt32(oneBytes);
                        int uinameLength = BitConverter.ToInt32(uinameLengthBytes);

                        // 检查每个标签的格式是否正确
                        if (lbl != " LBL" || one != 1)
                        {
                            throw new InvalidDataException("Invalid label format"); // 格式错误抛出异常
                        }

                        byte[] uiNameBytes = csfFile.ReadBytes(uinameLength);
                        byte[] rtsIdBytes = csfFile.ReadBytes(4);
                        string uiName = Encoding.UTF8.GetString(uiNameBytes).TrimEnd('\0');
                        string rtsId = Encoding.ASCII.GetString(rtsIdBytes);

                        uint rtsLen = csfFile.ReadUInt32() * 2;
                        byte[] contentRaw = csfFile.ReadBytes((int)rtsLen);

                        if (rtsId != " RTS")
                        {
                            uint extraLen = csfFile.ReadUInt32();
                            byte[] extraRaw = csfFile.ReadBytes((int)extraLen);
                        }

                        string content = BytesToString(contentRaw);
                        nameStrMap[uiName] = content;

                    }

                }

                return nameStrMap;
            }
            catch (Exception ex)
            { 
                return null; 
            }
        }

        // 将字节数组转换为字符串
        static string BytesToString(byte[] content)
        {
            byte[] strInvert = new byte[content.Length];
            for (int idx = content.Length - 1; idx >= 0; idx--)
            {
                strInvert[idx] = (byte)(content[idx] ^ 0xFF); // 对字节进行 XOR 操作
            }

            string result = Encoding.Unicode.GetString(strInvert); // 将处理后的字节数组转换为字符串
            return result;
        }

        // 比较两个字节数组是否相等
        static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        // 将字典中的数据写入 CSF 文件
        public static void WriteCSF(Dictionary<string, string> csfDictionary, string csfFilename)
        {
            if(csfDictionary == null) { return; }

            using (BinaryWriter csfFile = new BinaryWriter(File.Open(csfFilename, FileMode.Create)))
            {
                // 写入文件头部
                csfFile.Write(Encoding.ASCII.GetBytes(" FSC")); // 文件标识
                csfFile.Write((uint)3); // 版本号
                csfFile.Write((uint)csfDictionary.Count); // 标签数
                csfFile.Write((uint)csfDictionary.Count); // 字符串数
                csfFile.Write((uint)0); // 未使用的字段
                csfFile.Write((uint)0); // 语言 ID

                // 遍历字典并写入每个条目
                foreach (var item in csfDictionary)
                {
                    string uiName = item.Key;
                    string content = item.Value;

                    csfFile.Write(Encoding.ASCII.GetBytes(" LBL")); // 标签前缀
                    csfFile.Write((uint)1);
                    csfFile.Write((uint)uiName.Length); // UI 名称长度
                    csfFile.Write(Encoding.UTF8.GetBytes(uiName)); // UI 名称
                    csfFile.Write(Encoding.ASCII.GetBytes(" RTS")); // 内容前缀

                    byte[] contentBytes = StringToBytes(content); // 将字符串转换为字节数组
                    csfFile.Write((uint)contentBytes.Length / 2); // 写入内容长度
                    csfFile.Write(contentBytes); // 写入内容
                }
            }
        }

        // 将字符串转换为 CSF 文件格式的字节数组
        static byte[] StringToBytes(string str)
        {
            var contentBytes = Encoding.Unicode.GetBytes(str); // 将字符串转换为字节数组
            var strInvert = new byte[contentBytes.Length];
            for (int idx = 0; idx < contentBytes.Length; idx++)
            {
                strInvert[idx] = (byte)(contentBytes[idx] ^ 0xFF); // 对每个字节进行 XOR 操作
            }
            return strInvert;
        }
    }
}
