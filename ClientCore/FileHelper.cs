using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore
{
    public static class FileHelper
    {
        public static void CopyDirectory(string sourceDirPath, string saveDirPath)
        {
            if (!string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath))
            {
                if (!Directory.Exists(saveDirPath))
                {
                    Directory.CreateDirectory(saveDirPath);
                }

                // 复制文件
                string[] files = Directory.GetFiles(sourceDirPath);
                foreach (string file in files)
                {
                    string pFilePath = Path.Combine(saveDirPath, Path.GetFileName(file));
                    File.Copy(file, pFilePath, true);
                }

                // 递归复制子文件夹
                string[] directories = Directory.GetDirectories(sourceDirPath);
                foreach (string directory in directories)
                {
                    string pDirPath = Path.Combine(saveDirPath, Path.GetFileName(directory));
                    CopyDirectory(directory, pDirPath);
                }
            }
        }

        public static void DelFiles(List<string> deleteFiles)
        {
            if (deleteFiles != null)
            {
                foreach (string file in deleteFiles)
                {
                    try
                    {   if(!string.IsNullOrEmpty(file))
                            File.Delete(file);
                    }
                    catch
                    {
                        throw new FileLockedException($"文件操作失败，可能是这个文件{file}被占用了，等待几秒重试，若反复出现此问题可联系作者");
                    }
                }
            }
        }


        public static void ReNameCustomFile(bool Online = false)
        {
            List<string> customFile = [
                "custom_art_all.ini", 
                "custom_art_ra2.ini",
                "custom_art_yr.ini",
                "custom_rules_all.ini",
                "custom_rules_ra2.ini",
                "custom_rules_yr.ini"
                ];

            var tag = Online ? string.Empty : "Online";
            var tag2 = Online ? "Online" :  string.Empty;

            foreach (var fileName in customFile)
            {
                var ini = $"{ProgramConstants.GamePath}Client/{tag}{fileName}";

                if (File.Exists(ini))
                {
                    File.Move(ini, $"{ProgramConstants.GamePath}Client/{tag2}{fileName}",true);
                }
            }
        }
    }
}


