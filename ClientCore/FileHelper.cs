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

                string[] files = Directory.GetFiles(sourceDirPath);
                foreach (string file in files)
                {
                    string pFilePath = Path.Combine(saveDirPath, Path.GetFileName(file));
                    // 不会把map文件和ini复制到根目录，不然会影响任务包或MOD判定。
                    string extension = Path.GetExtension(file);
                    if (extension != ".map" && extension != ".png" && extension != ".ini" || Path.GetFileName(file).StartsWith("uimd") )
                    {
                        try
                        {
                            if(File.Exists(pFilePath))
                            {
                                File.SetAttributes(pFilePath, FileAttributes.Normal);
                                File.Delete(pFilePath);
                            }
                                
                            File.Copy(file, pFilePath, true);
                        }
                        catch
                        {
                            throw new FileLockedException($"文件操作失败，可能是这个文件{file}被占用了，等待几秒重试，若反复出现此问题可联系作者");
                        }
                    }
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
                if (File.Exists($"Client/{fileName}"))
                {
                    File.Move($"Client/{tag}{fileName}", $"Client/{tag2}{fileName}",true);
                }
            }
        }
    }
}


